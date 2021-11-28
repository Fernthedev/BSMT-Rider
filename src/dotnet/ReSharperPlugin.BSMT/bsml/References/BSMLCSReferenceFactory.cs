using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Collections;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.RiderTutorials.Utils;
using JetBrains.Util;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLCSReferenceFactory : IReferenceFactory
    {
        private readonly ICSharpFile _cSharpFile;
        private readonly Dictionary<ITypeDeclaration, IXmlFile> _typesToBsml = new();
        private readonly BsmlFileManager _bsmlFileManager;
        private readonly BsmlReferenceProviderFactory _bsmlReferenceProviderFactory;

        private readonly Dictionary<IAttribute, string> _attributeToNameMap = new();


        private readonly SemaphoreSlim _slim = new(1, 1);

        public BSMLCSReferenceFactory(ICSharpFile cSharpFile, BsmlFileManager bsmlFileManager, BsmlReferenceProviderFactory bsmlReferenceProviderFactory)
        {
            _cSharpFile = cSharpFile;
            _bsmlFileManager = bsmlFileManager;
            _bsmlReferenceProviderFactory = bsmlReferenceProviderFactory;
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
            var project = _cSharpFile.GetProject();
            if (project is null)
                return;

            var sourceFile = _cSharpFile.GetSourceFile();

            if (sourceFile is null)
                return;

            var elementToMap = _bsmlFileManager.GetAssociatedBsmlFiles(_cSharpFile);
            
            // get BSML annotations

            // var attributes = _cSharpFile.GetTypeInFile<IAttribute>();
            // var definedValues = attributes
            //     .Where((attribute, _) =>
            //     {
            //         if (attribute is null || !attribute.IsValid())
            //         {
            //             return false;
            //         }
            //
            //         if (attribute.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass)
            //             return false;
            //
            //         return IsClrTypeBsmlToSource(attributeClass.GetClrName()) ||
            //                IsClrTypeSourceToBsml(attributeClass.GetClrName());
            //     })
            //     .ToDictionary(attribute => attribute, attribute => attribute.ConstructorArgumentExpressionsEnumerable.SingleItem!.ConstantValue.Value as string);
            //
            
            // _attributeToNameMap.Clear();
            // foreach (var (attribute, name) in definedValues)
            // {
            //     if (name is not null)
            //     {
            //         _attributeToNameMap[attribute] = name;
            //     }
            // }

            _attributeToNameMap.Clear();
            _typesToBsml.Clear();
            foreach (var (key, value) in elementToMap)
            {
                if (value is null) continue;
                
                _typesToBsml[key] = value;
                    
                // Find XML file references
                var factory = _bsmlReferenceProviderFactory.GetXmlReferenceFactory(value);

                if (factory is null) continue;


                var attributes = value.GetChildrenInSubtreesUnrecursive<IAttribute>();


                ParseAttributes(attributes);
            }

            _slim.Release();
        }

        private void ParseAttributes(IEnumerable<IAttribute> attributes)
        {
            foreach (var attribute in attributes
                .Where(attribute =>
                {
                    if (attribute.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass)
                        return false;

                    return IsClrTypeBsmlToSource(attributeClass.GetClrName()) ||
                           IsClrTypeSourceToBsml(attributeClass.GetClrName());
                }))
            {

                var value = attribute.ConstructorArgumentExpressionsEnumerable.SingleItem!.ConstantValue
                    .Value as string;

                if (value is null)
                    continue;

                _attributeToNameMap[attribute] = value;
            }
        }


        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            // TODO: Use oldReferences cache
            if (element is not ILiteralExpression literal || literal.ConstantValue.Value is not string)
                return ReferenceCollection.Empty;

            var argumentExpression = literal as ICSharpExpression;
            var attribute = AttributeNavigator.GetByConstructorArgumentExpression(argumentExpression);

            if (attribute?.Name.Reference.Resolve().DeclaredElement is not IClass attributeClass)
                return ReferenceCollection.Empty;

            var clrType = attribute.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>();

            if (clrType is null)
                return ReferenceCollection.Empty;

            var attributeClassClrName = attributeClass.GetClrName();

            var doUpdateReferences =
                Equals(attributeClassClrName, BSMLConstants.BsmlViewDefinitionAttribute) ||
                IsClrTypeBsmlToSource(attributeClassClrName) ||
                IsClrTypeSourceToBsml(attributeClassClrName);

            if (!doUpdateReferences)
            {
                return ReferenceCollection.Empty;
            }

            UpdateReferences();

            if (_typesToBsml.IsEmpty() || !_typesToBsml.TryGetValue(clrType, out var bsmlFile))
            {
                return ReferenceCollection.Empty;
            }

            var newReferences = ReferenceCollection.Empty;

            if (IsClrTypeSourceToBsml(attributeClassClrName))
            {
                newReferences = SourceToBsmlTagReference(literal, bsmlFile);
            }
            else if (IsClrTypeBsmlToSource(attributeClassClrName))
            {
                newReferences = BsmlToSourceTagReference(literal, bsmlFile);
            }
            else if (Equals(attributeClassClrName, BSMLConstants.BsmlViewDefinitionAttribute))
            {
                newReferences = ClassToBsmlFileReference(literal, bsmlFile);
            }

            return ResolveUtil.ReferenceSetsAreEqual(newReferences, oldReferences) ? oldReferences : newReferences;
        }

        private ReferenceCollection BsmlToSourceTagReference(ILiteralExpression attributeLiteral, IXmlFile bsmlFile)
        {
            var attributeToCsVariable = _bsmlFileManager.ParseBsml(bsmlFile)!.GetAttributeToNameMap();

            var tagDictionary =
                attributeToCsVariable
                    .Where(pair => attributeLiteral.ConstantValue.Value as string == BSMLConstants.StripPrefix(pair.Value))
                    .Select(pair => new Tuple<IXmlTag, string>(pair.Key.GetParentOfType<IXmlTag>(), BSMLConstants.StripPrefix(pair.Value)))
                    .ToList();

            if (tagDictionary.IsEmpty())
                return ReferenceCollection.Empty;

            return new ReferenceCollection(
                tagDictionary.Select(tuple => new SourceToBsmlTagReference(attributeLiteral, tuple)).ToArray<IReference>()
            );
        }
        
        private ReferenceCollection SourceToBsmlTagReference(ILiteralExpression literal, IXmlFile bsmlFile)
        {
            var identifiedTags = _bsmlFileManager.ParseBsml(bsmlFile)!.GetIdentifiedTags();
            
            // if (identifiedTags.IsEmpty())
            //     return ReferenceCollection.Empty;

            var identifiedTagsTuple = identifiedTags.Select(pair => new Tuple<IXmlTag, string>(pair.Value, pair.Key));


            return new ReferenceCollection(
                identifiedTagsTuple.Select(tuple => new SourceToBsmlTagReference(literal, tuple)).ToArray<IReference>()
                // new SourceToBsmlTagReference(literal, identifiedTagsTuple)
            );
        }

        private static ReferenceCollection ClassToBsmlFileReference(ILiteralExpression literal, IXmlFile bsmlFile)
        {
            var attributeValue = literal.ConstantValue.Value as string;

            var xmlTag = bsmlFile.GetTag(tag => tag is not null);
            
            if (xmlTag is null)
                return ReferenceCollection.Empty;

            var tuple = new Tuple<IXmlTag, string>(xmlTag, attributeValue!);

            return new ReferenceCollection(
                new SourceToBsmlTagReference(literal, tuple)
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