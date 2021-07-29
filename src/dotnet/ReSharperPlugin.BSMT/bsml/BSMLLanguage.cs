using JetBrains.ReSharper.Psi.Xml;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    // Is this needed?
    // It does nothing at the moment
    // [LanguageDefinition(Name)]
    public class BSMLLanguage : XmlLanguage
    {
        public new const string Name = "BSML";

        protected BSMLLanguage() : base(Name, "BSML")
        {
        }
    }
}