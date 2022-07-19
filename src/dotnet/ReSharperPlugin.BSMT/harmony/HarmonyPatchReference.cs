using System;
using System.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.BSMT_Rider.harmony;

public class HarmonyPatchReference : TreeReferenceBase<ITypeDeclaration>
{
    // public HarmonyPatchReference(IClrTypeName target, ICSharpExpression literal)
    // {
    // }

    private readonly ITypeElement _typeElement;
    private readonly string _targetValue;
    
    public HarmonyPatchReference(ITypeDeclaration owner, string targetValue) : base(owner)
    {
        Debug.Assert(owner.DeclaredElement != null, "owner.DeclaredElement != null");
        _targetValue = targetValue;
        _typeElement = owner.DeclaredElement?.GetContainingType() ?? throw new InvalidOperationException();
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        return GetReferenceSymbolTable(true).GetResolveResult(GetName());
    }

    public override string GetName()
    {
        return _targetValue;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var symbolTable = ResolveUtil
            .GetSymbolTableByTypeElement(_typeElement,
                SymbolTableMode.FULL,
                _typeElement.Module);

        symbolTable = symbolTable.Distinct(); // .Filter(new PredicateFilter());

        return useReferenceName
            ? symbolTable.Filter(GetName(), new ExactNameFilter(_targetValue))
            : symbolTable;
    }

    public override TreeTextRange GetTreeTextRange()
    {
        return GetTreeNode().GetTreeTextRange();
    }

    public override IReference BindTo(IDeclaredElement element)
    {
        return this;
    }

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
        return this;
    }

    public override IAccessContext GetAccessContext()
    {
        return new ElementAccessContext(myOwner);
    }


}