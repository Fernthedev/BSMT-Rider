using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLCSReferenceFactory : IReferenceFactory
    {
        private readonly ICSharpFile _cSharpFile;
        private readonly Dictionary<ITypeDeclaration, BsmlClassData> _typesToBsml = new();
        private readonly BsmlFileManager _bsmlFileManager;
        private readonly BsmlReferenceProviderFactory _bsmlReferenceProviderFactory;
        

        public BSMLCSReferenceFactory(ICSharpFile cSharpFile, BsmlFileManager bsmlFileManager, BsmlReferenceProviderFactory bsmlReferenceProviderFactory)
        {
            _cSharpFile = cSharpFile;
            _bsmlFileManager = bsmlFileManager;
            _bsmlReferenceProviderFactory = bsmlReferenceProviderFactory;
        }



        /// <summary>
        /// Reads the class' [ViewDefinition("assemblyName")] attribute, then finds the XML file and
        /// associates the attribute's attached class to the file
        /// </summary>
        private void UpdateReferences()
        {
            var project = _cSharpFile.GetProject();
            if (project is null)
                return;

            var sourceFile = _cSharpFile.GetSourceFile();

            if (sourceFile is null)
                return;

            var elementToMap = _bsmlFileManager.GetAssociatedBsmlFiles(_cSharpFile);
            
            _typesToBsml.Clear();
            foreach (var (key, value) in elementToMap)
            {
                if (value is null) continue;
                
                _typesToBsml[key] = value;
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
                BSMLConstants.IsClrTypeBsmlToSource(attributeClassClrName) ||
                BSMLConstants.IsClrTypeSourceToBsml(attributeClassClrName);

            if (!doUpdateReferences)
            {
                return ReferenceCollection.Empty;
            }

            UpdateReferences();

            if (_typesToBsml.IsEmpty() || !_typesToBsml.TryGetValue(clrType, out var bsmlFile))
            {
                return new ReferenceCollection();
            }

            var newReferences = ReferenceCollection.Empty;

            if (BSMLConstants.IsClrTypeSourceToBsml(attributeClassClrName))
            {
                newReferences = SourceToBsmlTagReference(literal, bsmlFile.AssociatedBsmlFile!);
            }
            else if (BSMLConstants.IsClrTypeBsmlToSource(attributeClassClrName))
            {
                newReferences = BsmlToSourceTagReference(literal, bsmlFile.AssociatedBsmlFile!);
            }
            else if (Equals(attributeClassClrName, BSMLConstants.BsmlViewDefinitionAttribute))
            {
                newReferences = ClassToBsmlFileReference(literal, bsmlFile.AssociatedBsmlFile!);
            }

            return ResolveUtil.ReferenceSetsAreEqual(newReferences, oldReferences) ? oldReferences : newReferences;
        }

        private ReferenceCollection BsmlToSourceTagReference(ILiteralExpression attributeLiteral, IXmlFile bsmlFile)
        {
            var attributeToCsVariable = _bsmlFileManager.ParseBsml(bsmlFile)!.GetAttributeToNameMap();

            var tagDictionary =
                attributeToCsVariable
                    .Where(pair => attributeLiteral.ConstantValue.Value as string == BSMLConstants.StripGarbage(pair.Value))
                    .Select(pair => new Tuple<IXmlTag?, string>(pair.Key.GetParentOfType<IXmlTag>(), BSMLConstants.StripGarbage(pair.Value)))
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


            return new ReferenceCollection(
                identifiedTags.Select(tuple => new SourceToBsmlTagReference(literal, tuple)).ToArray<IReference>()
            );
        }

        private static ReferenceCollection ClassToBsmlFileReference(ILiteralExpression literal, IXmlFile bsmlFile)
        {
            var attributeValue = literal.ConstantValue.Value as string;

            var xmlTag = bsmlFile.GetTag(tag => tag is not null);
            
            if (xmlTag is null)
                return ReferenceCollection.Empty;

            return new ReferenceCollection(
                new SourceToBsmlTagReference(literal, attributeValue!, xmlTag)
            );
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