using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLFileManager
    {
        private readonly Dictionary<VirtualFileSystemPath, IXmlFile?> _xmlFiles = new();

        private readonly Dictionary<IPsiSourceFile, DateTime> _modificationMap = new();

        private readonly Dictionary<ITypeDeclaration, IXmlFile?> _classToBsml = new();

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

        private bool HasBeenModified(IPsiSourceFile file)
        {
            if (!_modificationMap.TryGetValue(file, out var lastModifiedTime))
                return true;

            var modifiedTime = file.LastWriteTimeUtc;

            return modifiedTime != lastModifiedTime;
        }

        private void MarkModification(IPsiSourceFile file)
        {
            _modificationMap[file] = file.LastWriteTimeUtc;
        }

        /// <summary>
        ///
        /// Returns a dictionary association of a C# class -> XmlFile
        ///
        /// a XmlFile value can be null if the C# class references a BSML file that could not be found
        ///
        /// </summary>
        /// <param name="cSharpFile"></param>
        /// <returns></returns>
        public Dictionary<ITypeDeclaration, IXmlFile?> GetAssociatedBSMLFile(ICSharpFile cSharpFile)
        {
            var project = cSharpFile.GetProject()!;
            var sourceFile = cSharpFile.GetSourceFile()!;

            var attributes = cSharpFile.GetTypeInFile<IAttribute>();

            if (attributes.IsEmpty())
                return new Dictionary<ITypeDeclaration, IXmlFile?>();

            if (!HasBeenModified(sourceFile))
            {
                Dictionary<ITypeDeclaration, IXmlFile?> map = new();

                foreach (var parentClazz in attributes.Select(attribute => attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()!))
                {
                    if (_classToBsml.TryGetValue(parentClazz, out var bsmlFile))
                    {
                        map[parentClazz] = bsmlFile;
                    }
                }

                return map;
            }

            Dictionary<IAttribute, string> definedViews = attributes
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
                    new TupleStruct<IAttribute, string?>(attribute.Item1,
                        attribute.Item1.ConstructorArgumentExpressions.FirstOrDefault()?.ConstantValue.Value as string))

                // Ensure is not null;
                .Where(e => e.Item2 is not null)
                .ToDictionary(e => e.Item1, e => e.Item2!);

            // Class:path_to_view_file
            Dictionary<ITypeDeclaration, VirtualFileSystemPath> mappedViews = new();

            // Add attribute views
            foreach (var (attribute, path) in definedViews)
            {
                try
                {
                    var assembly = ParseBsmlAssemblyPath(project, path);

                    mappedViews[attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()!] = assembly; //.EvaluateFileSystemPath();
                }
                catch (Exception)
                {
                    // ignored
                }
            }


            // Add Implicit view
            try
            {
                var bsmlLocalPath = VirtualFileSystemPath.ParseRelativelyTo(
                    Path.ChangeExtension(sourceFile.Name, "bsml"),
                    sourceFile.GetLocation().Parent);


                foreach (var e in
                    cSharpFile.GetTypeInFile<ITypeDeclaration>().ToList()
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

            Dictionary<ITypeDeclaration, IXmlFile?> elementToMap = new();

            foreach (var (element, path) in mappedViews)
            {
                if (!_xmlFiles.TryGetValue(path, out var xmlFile))
                {
                    var psiSourceFiles = project.GetPsiSourceFilesInProject(path);

                    var psiSourceFile = psiSourceFiles.FirstOrDefault(file => file is not null && file.PrimaryPsiLanguage is not UnknownLanguage);

                    if (psiSourceFile is not null)
                    {
                        xmlFile = psiSourceFile.GetPsiFiles<XmlLanguage>().SafeOfType<IXmlFile>().SingleOrDefault();
                    }
                }

                if (xmlFile is null)
                {
                    _xmlFiles[path] = null;
                }
                else
                {
                    elementToMap[element] = xmlFile;
                }
            }

            foreach (var (clazz, file) in elementToMap)
            {
                _classToBsml[clazz] = file;
            }

            MarkModification(sourceFile);

            return elementToMap;
        }

        public List<ITypeDeclaration> FindClassesAssociatedToBSMLFile(IXmlFile xmlFile)
        {
            List<ITypeDeclaration> declarations = new();

            foreach (var (clazz, bsmlFile) in _classToBsml)
            {
                if (bsmlFile == xmlFile)
                    declarations.Add(clazz);
            }

            return declarations;
        }

        // /// <summary>
        // ///
        // /// Returns a dictionary association of a C# class -> XmlFile
        // ///
        // /// a XmlFile value can be null if the C# class references a BSML file that could not be found
        // ///
        // /// </summary>
        // /// <param name="cSharpFile"></param>
        // /// <returns></returns>
        // public Dictionary<ITypeDeclaration, IXmlFile?> GetAssociatedBSMLFile(ITypeDeclaration clazz, IAttribute viewDefinitionAttribute, bool validate = true)
        // {
        //     if (validate)
        //     {
        //         var clrType = viewDefinitionAttribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>();
        //
        //         if (clrType is null)
        //             throw new ArgumentException($"{viewDefinitionAttribute} does not have type declaration?");
        //
        //         if (viewDefinitionAttribute?.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass)
        //         {
        //             throw new ArgumentException("what");
        //         }
        //
        //         var attributeClassClrName = attributeClass.GetClrName();
        //
        //         if (!Equals(attributeClassClrName, BSMLConstants.BsmlViewDefinitionAttribute))
        //             throw new ArgumentException(
        //                 $"{viewDefinitionAttribute} is not expected {BSMLConstants.BsmlViewDefinitionAttribute}");
        //     }
        //
        //     var viewPath = viewDefinitionAttribute.ConstructorArgumentExpressions.FirstOrDefault()?.ConstantValue.Value as string;
        //
        // }
    }
}