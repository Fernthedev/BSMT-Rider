using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.UnitTesting.Analysis.Xunit.References;
using JetBrains.Util;
using ReSharperPlugin.BSMT_Rider.utils;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BsmlXmlReferenceFactory : IReferenceFactory
    {
        private readonly IXmlFile _xmlFile;
        private readonly BsmlFileManager _bsmlFileManager;
        private readonly BsmlReferenceProviderFactory _bsmlReferenceProviderFactory;
        private BsmlXmlData? _bsmlXmlData;
        
        /// <summary>
        /// C# classes that reference this BSML file
        /// </summary>
        private readonly List<ITypeDeclaration> _typeDeclarations = new();
        private readonly Dictionary<ITypeDeclaration, BSMLCSReferenceFactory> _bsmlcsReferenceFactories = new();

        public BsmlXmlReferenceFactory(IXmlFile xmlFile, BsmlFileManager bsmlFileManager, BsmlReferenceProviderFactory bsmlReferenceProviderFactory)
        {
            _xmlFile = xmlFile;
            _bsmlFileManager = bsmlFileManager;
            _bsmlReferenceProviderFactory = bsmlReferenceProviderFactory;
        }

        /// <summary>
        /// Reads the class' [ViewDefinition("assemblyName")] attribute, then finds the XML file and
        /// associates the attribute's attached class to the file
        /// </summary>
        private void UpdateReferences()
        {
            var project = _xmlFile.GetProject();
            if (project is null)
                return;

            var sourceFile = _xmlFile.GetSourceFile();

            if (sourceFile is null)
                return;
            

            // Find C# classes that reference this file, then get their reference factory

            _typeDeclarations.Clear();
            _typeDeclarations.AddRange(_bsmlFileManager.FindClassesAssociatedToBsmlFile(_xmlFile));
            
            _bsmlcsReferenceFactories.Clear();
            foreach (var typeDeclaration in _typeDeclarations)
            {
                var csSourceFile = typeDeclaration.GetSourceFile();
                var file = csSourceFile?.GetTheOnlyPsiFile<CSharpLanguage>();

                if (csSourceFile is null || !csSourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>() || file is not ICSharpFile cSharpFile) continue;
                
                // should never be null
                var factory = _bsmlReferenceProviderFactory.GetCsReferenceFactory(cSharpFile)!;
                _bsmlcsReferenceFactories[typeDeclaration] = factory;
            }
            
            _bsmlXmlData = _bsmlFileManager.ParseBsml(_xmlFile);
        }


        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (element is not IXmlTag tag)
            {
                return ReferenceCollection.Empty;
            }

            UpdateReferences();
            
            var newReferences = BsmlTagToSourceReference(tag);

            return ResolveUtil.ReferenceSetsAreEqual(newReferences, oldReferences) ? oldReferences : newReferences;
        }

        private ReferenceCollection BsmlTagToSourceReference(IXmlTag tag)
        {
            var attributes = tag.GetAttributes().ToList();
            var idAttribute = attributes.FirstOrDefault(BsmlFileManager.BsmlAttributeIdPredicate);
            if (idAttribute is not null)
            {
                var idValue = idAttribute.UnquotedValue;
                var attributeTargets = _bsmlFileManager.ClassToBsml.Where(attributeClassPair =>
                        attributeClassPair.Value.AssociatedBsmlFile == _xmlFile &&
                        attributeClassPair.Value.GetAttributeToNameMap().Values.Contains(idValue))
                    .SelectMany(attributeClassPair => attributeClassPair.Value.GetAttributeToNameMap());




                List<IReference> references = new();

                foreach (var (attribute, str) in attributeTargets)
                {

                    var fieldDecl = FieldDeclarationNavigator.GetByAttribute(attribute).FirstNotNull();

                    if (fieldDecl is null) continue;
                    
                    var fieldVal = attribute.GetChildrenInSubtrees<ILiteralExpression>()
                        .FirstOrDefault()!;

                    references.Add(new PropertyDataReference(fieldDecl.DeclaredElement!.ContainingType, fieldVal));
                }


                return new ReferenceCollection(
                    references.ToArray<IReference>()
                );
            }

            return ReferenceCollection.Empty;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            // Check it's a string literal, and the text of the
            // string literal is in the collection of names
            if (element is not (IXmlTag or IXmlAttribute)) return false;
            //
            // // Check if parent is BSML View
            // var clrType = literal.GetParentOfTypeRecursiveNotStupid<ITypeDeclaration>();
            // if (clrType != null && clrType.DeclaredElement!.GetAllSuperClasses()
            //     .Any(e => e.GetClrName().Equals(BSMLConstants.BsmlViewControllerParent)))
            // {
            //
            //     return names.Contains(value);
            // }

            return false;
        }
    }
}