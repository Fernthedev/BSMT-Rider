using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLTagElementType : DeclaredElementTypeBase
    {
        public static readonly BSMLTagElementType BSML_TAG = new BSMLTagElementType("tag");

        public BSMLTagElementType(string name) : base(name, null)
        {
        }

        protected override IDeclaredElementPresenter DefaultPresenter { get; }

    }
}