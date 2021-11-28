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
        private readonly BSMLFileManager _bsmlFileManager;

        private readonly SemaphoreSlim _slim = new(1, 1);

        public BSMLCSReferenceFactory(ICSharpFile cSharpFile, BSMLFileManager bsmlFileManager)
        {
            _cSharpFile = cSharpFile;
            _bsmlFileManager = bsmlFileManager;
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

            var elementToMap = _bsmlFileManager.GetAssociatedBSMLFile(_cSharpFile);

            _typesToBsml.Clear();
            foreach (var (key, value) in elementToMap)
            {
                if (value is not null)
                {
                    _typesToBsml[key] = value;
                }
            }

            _slim.Release();
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
            else if (Equals(attributeClassClrName, BSMLConstants.BsmlViewDefinitionAttribute))
            {
                newReferences = SourceToBsmlFileReference(literal, bsmlFile);
            }

            return ResolveUtil.ReferenceSetsAreEqual(newReferences, oldReferences) ? oldReferences : newReferences;
        }

        private ReferenceCollection SourceToBsmlTagReference(ILiteralExpression literal, IXmlFile _bsmlFile)
        {
            var tagPredicate = new Func<IXmlAttribute, bool>(attribute => attribute.AttributeName == "id");

            List<Tuple<IXmlTag, string>> identifiedTags = _bsmlFile.GetChildrenInSubtrees()
                .SafeOfType<IXmlTag>()
                .Select(tag => new Tuple<IXmlTag, string?>(tag, tag.GetAttributes().FirstOrDefault(tagPredicate)?.UnquotedValue))
                .Where(tuple => tuple.Item2 is not null)
                .ToList()!;

            if (identifiedTags.IsEmpty())
                return ReferenceCollection.Empty;

            return new ReferenceCollection(
                new SourceToBsmlTagReference(literal, identifiedTags, _bsmlFile.GetPsiServices(), _bsmlFile.GetSourceFile()!.Name)
            );
        }

        private static ReferenceCollection SourceToBsmlFileReference(ILiteralExpression literal, IXmlFile _bsmlFile)
        {
            var attributeValue = literal.ConstantValue.Value as string;

            var xmlTag = _bsmlFile.GetTag(tag => tag is not null);

            var tuple = new Tuple<IXmlTag, string>(xmlTag, attributeValue);

            return new ReferenceCollection(
                new SourceToBsmlTagReference(literal, tuple, _bsmlFile.GetPsiServices(), _bsmlFile.GetSourceFile()!.Name)
            );
        }

        private static bool IsClrTypeSourceToBsml(IClrTypeName other)
        {
            return BSMLConstants.SourceToBsmlAttributes.Any(a => Equals(a, other));
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