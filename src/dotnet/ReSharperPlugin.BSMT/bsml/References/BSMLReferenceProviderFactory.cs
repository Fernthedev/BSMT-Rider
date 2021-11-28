using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Collections;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.RiderTutorials.Utils;
using JetBrains.Util;
using ReSharperPlugin.BSMT_Rider.utils;
using Xunit.Sdk;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    [ReferenceProviderFactory]
    public class BsmlReferenceProviderFactory : IReferenceProviderFactory
    {
        private readonly ILogger _logger;// = JetBrains.Util.Logging._logger.GetLogger(nameof(BsmlReferenceProviderFactory));

        private readonly Dictionary<ICSharpFile, BSMLReferenceFactory> _referenceFactories = new();

        public BsmlReferenceProviderFactory(Lifetime lifetime, ILogger logger)
        {
            Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName!);
            _logger = logger;
        }



        // This code is such a mess
        // I hate it
        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            _logger.Info("source file language factory");

            var project = sourceFile.GetProject();

            if (project == null)
                return null;

            if (sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>() && file is ICSharpFile cSharpFile)
            {
                _logger.Info(
                    $"Looking for file {sourceFile.GetLocation().Parent.FullPath} {VirtualFileSystemPath.ParseRelativelyTo(Path.ChangeExtension(sourceFile.Name, "bsml"), sourceFile.GetLocation().Parent).FullPath}");



                // var attributes = csharpFile!.GetTypeInFile<IAttribute>();
                //
                // if (attributes.IsEmpty())
                //     return null;
                //
                // var definedViews = attributes.Select((attribute, _) =>
                //     {
                //         try
                //         {
                //             return new TupleStruct<IAttribute, IDeclaredElement>(attribute,
                //                 attribute?.Name.Reference.Resolve().DeclaredElement);
                //         }
                //         catch (Exception)
                //         {
                //         return default;
                //         }
                //     })
                //     .Where(e => e != default)
                //     // Get attribute's class
                //     .Where(e => e.Item2 is IClass)
                //     .Select(e => e.Cast<IAttribute, IClass>())
                //
                //     // Check if attribute is BSML view
                //     .Where(attribute => attribute.Item2.GetClrName().Equals(BSMLConstants.BsmlViewDefinitionAttribute))
                //
                //     // Get path to view
                //     .Select(attribute =>
                //         new TupleStruct<IAttribute, string>(attribute.Item1,
                //             attribute.Item1.ConstructorArgumentExpressions.FirstOrDefault()?.ConstantValue
                //                 .Value as string))
                //
                //     // Ensure is not null;
                //     .Where(e => e.Item2 != null)
                //     .ToDictionary(e => e.Item1, e => e.Item2);
                //
                // // Class:path_to_view
                // Dictionary<ITypeElement, FileSystemPath> mappedViews = new();
                //
                // // Add attribute views
                // if (!definedViews.IsEmpty())
                // {
                //     var assemblies = project.GetAllReferencedAssemblies();
                //     var assemblyNames = assemblies.SelectMany((assembly, _) => assembly.GetReferencedAssemblyNames())
                //         .ToList();
                //
                //
                //     foreach (var (attribute, path) in definedViews)
                //     {
                //         try
                //         {
                //             // project.FindProjectToAssembliesReferencePath()
                //             var assembly = assemblyNames.Find(e => e.FullName == path);
                //
                //             if (assembly is not null)
                //             {
                //                 mappedViews[attribute.GetParentOfTypeNotStupid<ITypeElement>()] =
                //                     assembly.EvaluateFileSystemPath();
                //             }
                //         }
                //         catch (Exception)
                //         {
                //             // ignored
                //         }
                //     }
                // }
                //
                // // Add Implicit view
                // try
                // {
                //     var bsmlLocalPath = FileSystemPath.ParseRelativelyTo(Path.ChangeExtension(sourceFile.Name, "bsml"),
                //         sourceFile.GetLocation().Parent);
                //
                //     csharpFile.GetTypeInFile<ITypeDeclaration>().ToList()
                //         .Where(type =>
                //             type.DeclaredElement != null &&
                //             !mappedViews.ContainsKey(type.DeclaredElement) &&
                //             type.SuperTypes.Any(e => e.GetClrName().Equals(BSMLConstants.BSMLViewControllerParent))
                //         ).ToList().ForEach(e =>
                //             mappedViews[e.DeclaredElement!] = bsmlLocalPath
                //         );
                // }
                // catch (Exception)
                // {
                //     // ignored
                // }
                //
                // Dictionary<FileSystemPath, IXmlFile> xmlFiles = new();
                // Dictionary<ITypeElement, IXmlFile> elementToMap = new();
                //
                // foreach (var (element, path) in mappedViews)
                // {
                //     if (!xmlFiles.TryGetValue(path, out var xmlFile))
                //     {
                //         var psiSourceFile = project.GetPsiSourceFileInProject(path);
                //
                //         if (psiSourceFile?.PrimaryPsiLanguage is not UnknownLanguage)
                //         {
                //             xmlFile = sourceFile.GetPsiFiles<XmlLanguage>().SafeOfType<IXmlFile>().SingleOrDefault();
                //         }
                //     }
                //
                //     if (xmlFile is null)
                //     {
                //         xmlFiles[path] = null;
                //     }
                //     else
                //     {
                //         elementToMap[element] = xmlFile;
                //     }
                // }

                if (!_referenceFactories.TryGetValue(cSharpFile, out var factory))
                {
                    _referenceFactories[cSharpFile] = factory = new BSMLReferenceFactory(cSharpFile);
                }

                return factory;
            }

            if (sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>() || sourceFile.PrimaryPsiLanguage.Is<BSMLLanguage>())
            {

            }

            return null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}