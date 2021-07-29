using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    [ProjectFileTypeDefinition(Name)]
    public class BSMLFileType : XmlProjectFileType
    {
        // TODO: Make this BSML when lang?
        public new const string Name = "BSML";
        public const string BSML_EXTENSION = ".bsml";

        [CanBeNull, UsedImplicitly]
        public new static BSMLFileType Instance { get; private set; }

        public BSMLFileType() :
            base(Name, "BSML", new[] {BSML_EXTENSION})
        {
        }
    }
}