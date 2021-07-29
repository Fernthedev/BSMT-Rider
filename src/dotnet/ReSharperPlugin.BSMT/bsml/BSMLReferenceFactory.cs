using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.RiderTutorials.Utils;
using JetBrains.Util;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLReferenceFactory : IReferenceFactory
    {
        private IXmlFile _bsmlFile;

        public BSMLReferenceFactory(IXmlFile bsmlFile)
        {
            _bsmlFile = bsmlFile;
        }

        public static readonly ClrTypeName BSMLViewControllerParent =
            new ClrTypeName("BeatSaberMarkupLanguage.BSMLViewController");

        private static readonly List<ClrTypeName> BsmlAttributes =
            new List<string>
            {
                // "UIValue",
                // "UIAction"
                "UIComponent"
            }.Select(s => new ClrTypeName($"BeatSaberMarkupLanguage.Attributes.{s}")).ToList();

        public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
        {
            if (element is not ILiteralExpression literal || literal.ConstantValue.Value is not string)
                return ReferenceCollection.Empty;

            var argumentExpression = literal as ICSharpExpression;
            var attribute = AttributeNavigator.GetByConstructorArgumentExpression(argumentExpression);

            var @class = attribute?.Name.Reference.Resolve().DeclaredElement as IClass;

            if (@class == null || !IsClrTypeBsml(@class.GetClrName()))
                return ReferenceCollection.Empty;


            var tagPredicate = new Func<IXmlAttribute, bool>(attribute => attribute.AttributeName == "id");

            var identifiedTags = _bsmlFile.GetChildrenInSubtrees()
                .SafeOfType<IXmlTag>()
                .Select(tag =>
                    new Tuple<IXmlTag, IXmlAttribute>(tag, tag.GetAttributes().Where(tagPredicate).FirstOrDefault()))
                .Where(tuple => tuple.Item2 != default)
                .ToList();
                // .ToDictionary(tag => tag.Item1, tag => tag.Item2);

            // List<string> namesOfIds = identifiedTags
            //     .Select(tag => tag.GetAttributes().FirstOrDefault(tagPredicate)?.UnquotedValue)
            //     .Where(id => id != null)
            //     .ToList();

            return new ReferenceCollection(
                // identifiedTags.Select(tag => new BSMLReference(literal, tag.Key, _bsmlFile.GetPsiServices(), tag.Value.UnquotedValue)).ToList()
                new BSMLReference(literal, identifiedTags, _bsmlFile.GetPsiServices(), _bsmlFile.GetSourceFile()!.Name)
            );

        }

        private static bool IsClrTypeBsml(IClrTypeName other)
        {
            return BsmlAttributes.Any(a => Equals(a, other));
        }

        public static T GetParentOfType<T>(ITreeNode node) where T : class
        {
            for (; node != null; node = node.Parent)
            {
                if (node is T obj1)
                    return obj1;
            }

            return default;
        }

        public bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            // Check it's a string literal, and the text of the
            // string literal is in the collection of names
            if (element is not ILiteralExpression literal || literal.ConstantValue.Value is not string value) return false;

            // Check if parent is BSML View
            var clrType = GetParentOfType<ITypeElement>(literal);
            if (clrType != null && clrType.GetAllSuperClasses()
                .Any(e => e.GetClrName().Equals(BSMLViewControllerParent)))
            {

                return names.Contains(value);
            }

            return false;
        }
    }
}