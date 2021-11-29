using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Collections;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    // This is the reference for the reference provider
    // Basically it tries to check if the code in C#
    // matches an id to the BSML code.
    // Auto complete should match for all possible references
    public class SourceToBsmlTagReference : TreeReferenceBase<ILiteralExpression>,
        ICompletableReference,
        IAccessContext
    {
        // tag:tag_id
        private readonly ISymbolTable _symbolTable;

        public SourceToBsmlTagReference(ILiteralExpression owner, IEnumerable<Tuple<IXmlTag, string>> tags) : base(owner)
        {
            List<Tuple<IXmlTag, string>> tags1 = tags.ToList();
            List<IDeclaredElement> elements = new(tags1.Select(tag => new BSMLTagElement(tag.Item1, tag.Item2)).ToList());
            _symbolTable = ResolveUtil.CreateSymbolTable(elements, 0);
        }
        
        public SourceToBsmlTagReference(ILiteralExpression owner, IReadOnlyDictionary<IXmlTag, string> tags) : base(owner)
        {
            List<IDeclaredElement> elements = new(tags.Select(tag => new BSMLTagElement(tag.Key, tag.Value)).ToList());
            _symbolTable = ResolveUtil.CreateSymbolTable(elements, 0);
        }

        public SourceToBsmlTagReference(ILiteralExpression owner, Tuple<IXmlTag, string> tag) : base(owner)
        {
            var (item1, item2) = tag;
            _symbolTable = ResolveUtil.CreateSingletonSymbolTable(new BSMLTagElement(item1, item2), EmptySubstitution.INSTANCE);
        }

        public SourceToBsmlTagReference(ILiteralExpression owner, KeyValuePair<IXmlTag, string> tag) : base(owner)
        {
            var (key, value) = tag;
            _symbolTable = ResolveUtil.CreateSingletonSymbolTable(new BSMLTagElement(key, value), EmptySubstitution.INSTANCE);
        }
        
        public SourceToBsmlTagReference(ILiteralExpression owner, Tuple<string, IXmlTag> tag) : base(owner)
        {
            var (item1, item2) = tag;
            _symbolTable = ResolveUtil.CreateSingletonSymbolTable(new BSMLTagElement(item2, item1), EmptySubstitution.INSTANCE);
        }
        
        public SourceToBsmlTagReference(ILiteralExpression owner, string name, IXmlTag tag) : base(owner)
        {
            _symbolTable = ResolveUtil.CreateSingletonSymbolTable(new BSMLTagElement(tag, name), EmptySubstitution.INSTANCE);
        }

        public SourceToBsmlTagReference(ILiteralExpression owner, KeyValuePair<string, IXmlTag> tag) : base(owner)
        {
            var (key, value) = tag;
            _symbolTable = ResolveUtil.CreateSingletonSymbolTable(new BSMLTagElement(value, key), EmptySubstitution.INSTANCE);
        }
        
        

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            return GetReferenceSymbolTable(true).GetResolveResult(GetName());
        }

        public override string GetName()
        {
            return (myOwner.ConstantValue.Value as string)!;
        }

        public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
        {
            var exactNameFilter = new ExactNameFilter(GetName());

            return useReferenceName
                ? _symbolTable.Filter(GetName(), exactNameFilter)
                : _symbolTable;
        }

        public override TreeTextRange GetTreeTextRange()
        {
            TreeOffset treeStartOffset = myOwner.GetTreeStartOffset() + 1;
            var end = treeStartOffset + ((string) myOwner.ConstantValue.Value)!.Length;

            return new TreeTextRange(treeStartOffset, end);
        }

        public override IReference BindTo(IDeclaredElement element)
        {
            var literalAlterer = StringLiteralAltererUtil
                .CreateStringLiteralByExpression(myOwner);

            literalAlterer.Replace(((string) myOwner.ConstantValue.Value)!,
                element.ShortName);

            var newOwner = literalAlterer.Expression;
            if (!myOwner.Equals(newOwner))
                return newOwner.FindReference<SourceToBsmlTagReference>() ?? this;
            return this;
        }

        public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
        {
            return BindTo(element);
        }

        public override IAccessContext GetAccessContext() => this;

        public ISymbolTable GetCompletionSymbolTable()
        {
            return GetReferenceSymbolTable(false);
        }

        public ITypeElement GetAccessContainingTypeElement() => GetQualifierTypeElement();

        public Staticness GetStaticness() => Staticness.Any;

        public QualifierKind GetQualifierKind() => QualifierKind.OBJECT;

        public ITypeElement GetQualifierTypeElement() => null;

        public IPsiModule GetPsiModule()
        {
            return myOwner.GetPsiModule();
        }
    }
}