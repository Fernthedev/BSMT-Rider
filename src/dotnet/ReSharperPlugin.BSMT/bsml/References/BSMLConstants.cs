using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.Impl;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLConstants
    {
        public static readonly ClrTypeName BsmlViewDefinitionAttribute =
            new("BeatSaberMarkupLanguage.Attributes.ViewDefinitionAttribute");

        public static readonly ClrTypeName BsmlViewControllerParent = new("BeatSaberMarkupLanguage.ViewControllers.BSMLViewController");

        public static readonly List<ClrTypeName> SourceToBsmlAttributes =
            new List<string>
            {
                "UIComponent"
            }.Select(s => new ClrTypeName($"BeatSaberMarkupLanguage.Attributes.{s}")).ToList();

        public static readonly List<ClrTypeName> BsmlToSourceAttributes =
            new List<string>
            {
                "UIValue",
                "UIAction"
            }.Select(s => new ClrTypeName($"BeatSaberMarkupLanguage.Attributes.{s}")).ToList();
    }
}