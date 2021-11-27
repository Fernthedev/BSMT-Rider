using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.Collections;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Search;
using JetBrains.ProjectModel.Update;
using JetBrains.ReSharper.Features.Xaml.Previewer.Host.Extensions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xaml.Impl.Util;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.RiderTutorials.Utils;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLReferenceFactory : IReferenceFactory
    {
        private readonly ICSharpFile _cSharpFile;
        private readonly Dictionary<ITypeDeclaration, IXmlFile> _typesToBsml = new();
        private static readonly Dictionary<VirtualFileSystemPath, IXmlFile> XmlFiles = new();

        private readonly SemaphoreSlim _slim = new(1, 1);

        public BSMLReferenceFactory(ICSharpFile cSharpFile)
        {
            _cSharpFile = cSharpFile;
        }

        private async Task UpdateReferencesAsync()
        {
            // Do not update again unnecessarily, just wait for the first thread to finish
            if (!await _slim.WaitAsync(0))
            {
                if (await _slim.WaitAsync(7000))
                {
                    _slim.Release();
                }
                else
                {
                    Console.WriteLine("h");
                }

                return;
            }

            await _slim.WaitAsync();

            UpdateReferencesInternal();
        }

        private void UpdateReferences()
        {
            // Do not update again unnecessarily, just wait for the first thread to finish
            if (!_slim.Wait(0))
            {
                if (_slim.Wait(7000))
                {
                    _slim.Release();
                }
                else
                {
                    Console.WriteLine("h");
                }
                return;
            }

            UpdateReferencesInternal();
        }

        /// <summary>
        /// Reads the class' [ViewDefinition("assemblyName")] attribute, then finds the XML file and
        /// associates the attribute's attached class to the file
        /// </summary>
        private void UpdateReferencesInternal()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var project = _cSharpFile.GetProject();
            if (project is null)
                return;

            var sourceFile = _cSharpFile.GetSourceFile();

            if (sourceFile is null)
                return;

            var attributes = _cSharpFile!.GetTypeInFile<IAttribute>();

            if (attributes.IsEmpty())
                return;

            var definedViews = attributes
                .Where((attribute, _) => attribute is not null && attribute.IsValid())
                .Select((attribute, _) =>
                {
                    try
                    {
                        return new TupleStruct<IAttribute, IDeclaredElement>(attribute,
                            attribute.Name.Reference.Resolve().DeclaredElement);
                    }
                    catch (Exception)
                    {
                        return default;
                    }
                })
                // Get attribute's class
                .Where(e => e.Item2 is IClass)
                .Select(e => e.Cast<IAttribute, IClass>())

                // Check if attribute is BSML view
                .Where(attribute => attribute.Item2.GetClrName().Equals(BSMLConstants.BsmlViewDefinitionAttribute))

                // Get path to view
                .Select(attribute =>
                    new TupleStruct<IAttribute, string>(attribute.Item1,
                        attribute.Item1.ConstructorArgumentExpressions.FirstOrDefault()?.ConstantValue
                            .Value as string))

                // Ensure is not null;
                .Where(e => e.Item2 != null)
                .ToDictionary(e => e.Item1, e => e.Item2);

            // Class:path_to_view_file
            Dictionary<ITypeDeclaration, VirtualFileSystemPath> mappedViews = new();

            // Add attribute views
            if (!definedViews.IsEmpty())
            {
                // var assemblies = FileSystemPath.ParseRelativelyTo();
                // var assemblies = project.GetAllReferencedAssemblies();
                // var assemblyNames = assemblies.SelectMany((assembly, _) => assembly.GetReferencedAssemblyNames())
                // .ToList();


                foreach (var (attribute, path) in definedViews)
                {
                    try
                    {
                        // project.FindProjectToAssembliesReferencePath()
                        // var assembly = assemblyNames.Find(e => e.Name == path);

                        var assembly = ParseBsmlAssemblyPath(project, path);

                        // if (assembly is not null)
                        // {
                        mappedViews[attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()] = assembly; //.EvaluateFileSystemPath();
                        // }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            // Add Implicit view
            try
            {
                var bsmlLocalPath = VirtualFileSystemPath.ParseRelativelyTo(
                    Path.ChangeExtension(sourceFile.Name, "bsml"),
                    sourceFile.GetLocation().Parent);


                foreach (var e in
                    _cSharpFile.GetTypeInFile<ITypeDeclaration>().ToList()
                        .Where(type =>
                            type.DeclaredElement != null &&
                            !mappedViews.ContainsKey(type) &&
                            type.SuperTypes.Any(e => e.GetClrName().Equals(BSMLConstants.BsmlViewControllerParent))
                        ))
                {
                    mappedViews[e!] = bsmlLocalPath;
                }
            }
            catch (Exception)
            {
                // ignored
            }


            Dictionary<ITypeDeclaration, IXmlFile> elementToMap = new();

            foreach (var (element, path) in mappedViews)
            {
                if (!XmlFiles.TryGetValue(path, out var xmlFile))
                {
                    var psiSourceFiles = project.GetPsiSourceFilesInProject(path);

                    var psiSourceFile = psiSourceFiles?.FirstOrDefault(file => file is not null && file.PrimaryPsiLanguage is not UnknownLanguage);

                    if (psiSourceFile is not null)
                    {
                        xmlFile = psiSourceFile.GetPsiFiles<XmlLanguage>().SafeOfType<IXmlFile>().SingleOrDefault();
                    }
                }

                if (xmlFile is null)
                {
                    XmlFiles[path] = null;
                }
                else
                {
                    elementToMap[element] = xmlFile;
                }
            }

            _typesToBsml.Clear();
            foreach (var (key, value) in elementToMap)
            {
                _typesToBsml[key] = value;
            }

            stopwatch.Stop();
            var time = stopwatch.ElapsedMilliseconds;
            _slim.Release();
        }

        private static VirtualFileSystemPath ParseBsmlAssemblyPath(IProject project, string path)
        {
            var separator = Path.DirectorySeparatorChar.ToString();

            var assembly =
                VirtualFileSystemPath.ParseRelativelyTo(
                    path.Replace(project.GetOutputAssemblyName(TargetFrameworkId.Default) + ".", "")
                        .Replace(".", separator)
                        .ReplaceLastOccurrence(separator, "."),
                    project.ProjectFileLocation.Directory
                );

            return assembly;
        }

        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            // TODO: Use oldReferences cache
            if (element is not ILiteralExpression literal || literal.ConstantValue.Value is not string)
                return ReferenceCollection.Empty;

            var argumentExpression = literal as ICSharpExpression;
            var attribute = AttributeNavigator.GetByConstructorArgumentExpression(argumentExpression);

            if (attribute?.Name.Reference.Resolve().DeclaredElement is not IClass @class)
                return ReferenceCollection.Empty;

            var clrType = attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>();

            if (clrType is null)
                return ReferenceCollection.Empty;

            UpdateReferences();

            if (_typesToBsml.IsEmpty() || !_typesToBsml.TryGetValue(clrType, out var bsmlFile))
            {
                return ReferenceCollection.Empty;
            }

            var newReferences = ReferenceCollection.Empty;

            if (IsClrTypeBsmlToSource(@class.GetClrName()))
                newReferences = BsmlToSourceReference(literal);
            else
            if (IsClrTypeSourceToBsml(@class.GetClrName()))
                newReferences = SourceToBsmlReference(literal, bsmlFile);

            return ResolveUtil.ReferenceSetsAreEqual(newReferences, oldReferences) ? oldReferences : newReferences;
        }

        private ReferenceCollection BsmlToSourceReference(ILiteralExpression literal)
        {
            // var tagPredicate = new Func<IXmlAttribute, bool>(attribute => attribute.AttributeName == "id");
            //
            // var identifiedTags = _typesToBsml.GetChildrenInSubtrees()
            //     .SafeOfType<IXmlTag>()
            //     .Select(tag =>
            //         new Tuple<IXmlTag, IXmlAttribute>(tag, tag.GetAttributes().Where(tagPredicate).FirstOrDefault()))
            //     .Where(tuple => tuple.Item2 != default)
            //     .ToList();
            //
            // return new ReferenceCollection(
            //     new SourceToBSMLReference(literal, identifiedTags, _typesToBsml.GetPsiServices(), _typesToBsml.GetSourceFile()!.Name)
            // );

            return ReferenceCollection.Empty;
        }

        private ReferenceCollection SourceToBsmlReference(ILiteralExpression literal, IXmlFile _bsmlFile)
        {
            var tagPredicate = new Func<IXmlAttribute, bool>(attribute => attribute.AttributeName == "id");


            var identifiedTags = _bsmlFile.GetChildrenInSubtrees()
                .SafeOfType<IXmlTag>()
                .Select(tag =>
                    new Tuple<IXmlTag, IXmlAttribute>(tag, tag.GetAttributes().Where(tagPredicate).FirstOrDefault()))
                .Where(tuple => tuple.Item2 != default)
                .ToList();



            return new ReferenceCollection(
                new SourceToBSMLReference(literal, identifiedTags, _bsmlFile.GetPsiServices(), _bsmlFile.GetSourceFile()!.Name)
            );
        }

        private static bool IsClrTypeSourceToBsml(IClrTypeName other)
        {
            return BSMLConstants.SourceToBsmlAttributes.Any(a => Equals(a, other));
        }

        private static bool IsClrTypeBsmlToSource(IClrTypeName other)
        {
            return BSMLConstants.BsmlToSourceAttributes.Any(a => Equals(a, other));
        }



        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            // Check it's a string literal, and the text of the
            // string literal is in the collection of names
            if (element is not ILiteralExpression literal || literal.ConstantValue.Value is not string value) return false;

            // Check if parent is BSML View
            var clrType = literal.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>();
            if (clrType != null && clrType.DeclaredElement!.GetAllSuperClasses()
                .Any(e => e.GetClrName().Equals(BSMLConstants.BsmlViewControllerParent)))
            {

                return names.Contains(value);
            }

            return false;
        }
    }
}