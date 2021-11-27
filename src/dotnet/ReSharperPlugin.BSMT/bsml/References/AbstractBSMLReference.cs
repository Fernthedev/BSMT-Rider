using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    public abstract class AbstractBSMLReference<T> : TreeReferenceBase<ILiteralExpression>,
        ICompletableReference,
        IAccessContext where T : AbstractBSMLReference<T>
    {
        private readonly List<Tuple<IXmlTag, IXmlAttribute>> _tags;

        public AbstractBSMLReference([NotNull] ILiteralExpression owner, List<Tuple<IXmlTag, IXmlAttribute>> tags, IPsiServices psiServices, string name) : base(owner)
        {
            _tags = tags;
        }

        public override ResolveResultWithInfo ResolveWithoutCache()
        {
            return GetReferenceSymbolTable(true).GetResolveResult(GetName());
        }

        public override string GetName()
        {
            return (myOwner.ConstantValue.Value as string)!;
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
                return newOwner.FindReference<T>() ?? this;
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