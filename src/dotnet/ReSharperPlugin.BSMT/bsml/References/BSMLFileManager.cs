using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.RiderTutorials.Utils;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public record BsmlXmlData
    {
        private readonly ConcurrentDictionary<string, IXmlTag> _idToTag;
        private readonly ConcurrentDictionary<IXmlAttribute, string> _attributeToCsVariable;

        public BsmlXmlData(ConcurrentDictionary<string, IXmlTag> idToTag, ConcurrentDictionary<IXmlAttribute, string> attributeToCsVariable)
        {
            _idToTag = idToTag;
            _attributeToCsVariable = attributeToCsVariable;
        }
        
        public BsmlXmlData(Dictionary<string, IXmlTag> idToTag, Dictionary<IXmlAttribute, string> attributeToCsVariable)
        {
            _idToTag = new ConcurrentDictionary<string, IXmlTag>(idToTag);
            _attributeToCsVariable = new ConcurrentDictionary<IXmlAttribute, string>(attributeToCsVariable);
        }

        public IReadOnlyDictionary<string, IXmlTag> GetIdentifiedTags()
        {
            return _idToTag;
        }

        public IReadOnlyDictionary<IXmlAttribute, string> GetAttributeToNameMap()
        {
            return _attributeToCsVariable;
        }
    }

    public record BsmlClassData(IXmlFile? AssociatedBsmlFile)
    {
        private readonly ConcurrentDictionary<IAttribute, string> _attributeToNameMap = new();

        public BsmlClassData(IXmlFile? associatedBsmlFile, Dictionary<IAttribute, string> attributeToNameMap) : this(associatedBsmlFile)
        {
            _attributeToNameMap = new ConcurrentDictionary<IAttribute, string>(attributeToNameMap);
        }
        
        public BsmlClassData(IXmlFile? associatedBsmlFile, ConcurrentDictionary<IAttribute, string> attributeToNameMap) : this(associatedBsmlFile)
        {
            _attributeToNameMap = attributeToNameMap;
        }

        public IReadOnlyDictionary<IAttribute, string> GetAttributeToNameMap()
        {
            return _attributeToNameMap;
        }
    }
    
    public class BsmlFileManager
    {
        private readonly Dictionary<VirtualFileSystemPath, IXmlFile?> _xmlFiles = new();
        private readonly Dictionary<IPsiSourceFile, DateTime> _modificationMap = new();

        private readonly ConcurrentDictionary<ITypeDeclaration, BsmlClassData> _classToBsml = new();
        private readonly Dictionary<IXmlFile, BsmlXmlData> _bsmlXmlDatas = new();

        private readonly ConcurrentDictionary<object, SemaphoreSlim> _semaphoreSlims = new();

        public IReadOnlyDictionary<ITypeDeclaration, BsmlClassData> ClassToBsml => _classToBsml;

        private SemaphoreSlim GetSemaphore(object key)
        {
            var semaphore = _semaphoreSlims.GetOrAdd(key, _ => new SemaphoreSlim(1,1));

            return semaphore;
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

        private static Dictionary<IAttribute, string> ParseAttributes(IEnumerable<IAttribute> attributes)
        {
            Dictionary<IAttribute, string> attributeToNameMap = new();
            foreach (var attribute in attributes
                .Where(attribute =>
                {
                    if (attribute.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass)
                        return false;

                    return BSMLConstants.IsClrTypeBsmlToSource(attributeClass.GetClrName()) ||
                           BSMLConstants.IsClrTypeSourceToBsml(attributeClass.GetClrName());
                }))
            {

                var value = attribute.ConstructorArgumentExpressionsEnumerable.SingleItem!.ConstantValue
                    .Value as string;

                if (value is null)
                    continue;

                attributeToNameMap[attribute] = value;
            }

            return attributeToNameMap;
        }

        private static BsmlClassData ParseClass(ITypeDeclaration type, IXmlFile? file)
        {
            var attributes = type.GetChildrenInSubtreesUnrecursive<IAttribute>();
            
            return new BsmlClassData(file, ParseAttributes(attributes));
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
        public Dictionary<ITypeDeclaration, BsmlClassData> GetAssociatedBsmlFiles(ICSharpFile cSharpFile)
        {
            var project = cSharpFile.GetProject()!;
            var sourceFile = cSharpFile.GetSourceFile()!;

            var attributes = cSharpFile.GetTypeInFile<IAttribute>();

            if (attributes.IsEmpty())
                return new Dictionary<ITypeDeclaration, BsmlClassData>();

            Dictionary<ITypeDeclaration, BsmlClassData> FindCache()
            {

                Dictionary<ITypeDeclaration, BsmlClassData> map = new();

                foreach (var parentClazz in attributes.Select(attribute =>
                    attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()!))
                {
                    if (_classToBsml.TryGetValue(parentClazz, out var bsmlFile))
                    {
                        map[parentClazz] = bsmlFile;
                    }
                }

                return map;
            }

            if (!HasBeenModified(sourceFile))
            {
                return FindCache();
            }

            using var locker = Synchronization.Lock(GetSemaphore(cSharpFile), out bool locked);

            if (!locked)
            {
                return FindCache();
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

                    mappedViews[attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>()!] =
                        assembly; //.EvaluateFileSystemPath();
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

            Dictionary<ITypeDeclaration, BsmlClassData> elementToMap = new();

            foreach (var (element, path) in mappedViews)
            {
                if (!_xmlFiles.TryGetValue(path, out var xmlFile))
                {
                    var psiSourceFiles = project.GetPsiSourceFilesInProject(path);

                    var psiSourceFile = psiSourceFiles.FirstOrDefault(file =>
                        file is not null && file.PrimaryPsiLanguage is not UnknownLanguage);

                    if (psiSourceFile is not null)
                    {
                        xmlFile = psiSourceFile.GetPsiFiles<XmlLanguage>().SafeOfType<IXmlFile>().SingleOrDefault();
                    }
                }
                else
                {
                    if (xmlFile is not null && !xmlFile.GetSourceFile()!.IsValid())
                    {
                        xmlFile = null;
                    }
                }

                if (xmlFile is null)
                {
                    _xmlFiles[path] = null;
                }
                else
                {
                    elementToMap[element] = ParseClass(element, xmlFile);
                }
            }

            foreach (var (clazz, bsmlClassData) in elementToMap)
            {
                _classToBsml[clazz] = bsmlClassData;
            }

            MarkModification(sourceFile);

            return elementToMap;
        }

        public IEnumerable<ITypeDeclaration> FindClassesAssociatedToBsmlFile(IXmlFile xmlFile)
        {
            List<ITypeDeclaration> declarations = new();

            foreach (var (clazz, bsmlFile) in _classToBsml)
            {
                if (bsmlFile.AssociatedBsmlFile == xmlFile)
                    declarations.Add(clazz);
            }

            return declarations;
        }

        public static Func<IXmlAttribute, bool> BsmlAttributeIdPredicate => attribute => attribute.AttributeName == "id";

        public BsmlXmlData? ParseBsml(IXmlFile bsmlFile)
        {
            var project = bsmlFile.GetProject();
            if (project is null)
                return null;

            var sourceFile = bsmlFile.GetSourceFile();

            if (sourceFile is null)
                return null;

            _bsmlXmlDatas.TryGetValue(bsmlFile, out var bsmlXmlData);
            
            if (!HasBeenModified(sourceFile) && bsmlXmlData is not null)
                return bsmlXmlData;
            
            using var locker = Synchronization.Lock(GetSemaphore(bsmlFile), out var locked);

            if (!locked)
            {
                return _bsmlXmlDatas[bsmlFile];
            }

            // Parse XML file
            var tagPredicate = BsmlAttributeIdPredicate;

            var tags = bsmlFile.GetChildrenInSubtrees()
                .SafeOfType<IXmlTag>()
                .Where(tag => tag.IsValid())
                .ToList();

            Dictionary<string, IXmlTag> idToTag = new();

            foreach (var tag in tags)
            {
                var val = tag?.GetAttributes().FirstOrDefault(tagPredicate)?.UnquotedValue;

                if (tag is not null && val is not null)
                {
                    idToTag[val] = tag;
                }
            }


            // Find all attributes with "~" prefix
            Dictionary<IXmlAttribute, string> attributeToCsVariable = tags
                .SelectMany(tag => tag.GetAttributes().ToList())
                .Where(attribute => attribute.UnquotedValue.StartsWith(BSMLConstants.BsmlVarPrefix))
                .ToDictionary(attribute => attribute, attribute => attribute.UnquotedValue);
            

            _bsmlXmlDatas[bsmlFile] = bsmlXmlData = new BsmlXmlData(idToTag, attributeToCsVariable);

            MarkModification(sourceFile);

            return bsmlXmlData;
        }
        

        ~BsmlFileManager()
        {
            foreach (var semaphoreSlimsValue in _semaphoreSlims.Values)
            {
                semaphoreSlimsValue.Dispose();
            }
        }
    }
}