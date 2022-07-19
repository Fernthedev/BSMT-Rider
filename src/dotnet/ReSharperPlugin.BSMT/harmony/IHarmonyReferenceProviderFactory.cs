using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.BSMT_Rider.harmony
{
    [ReferenceProviderFactory]
    public class HarmonyReferenceProviderFactory : IReferenceProviderFactory
    {
        public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
        {
            return sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>() ? new HarmonyReferenceFactory() : null;
        }

        public ISignal<IReferenceProviderFactory> Changed { get; }
    }
}