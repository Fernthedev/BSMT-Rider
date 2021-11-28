using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.Impl;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLConstants
    {
        public const string BsmlAttributesNamespace = "BeatSaberMarkupLanguage.Attributes";
        public const string BsmlVarPrefix = "~";

        public static string StripPrefix(string str)
        {
            var containsPrefix = str.StartsWith(BsmlVarPrefix, StringComparison.OrdinalIgnoreCase);

            return !containsPrefix ? str : str[BsmlVarPrefix.Length..];
        }

        public static readonly ClrTypeName BsmlViewDefinitionAttribute =
            new($"{BsmlAttributesNamespace}.ViewDefinitionAttribute");

        public static readonly ClrTypeName BsmlViewControllerParent = new("BeatSaberMarkupLanguage.ViewControllers.BSMLViewController");

        public static readonly List<ClrTypeName> SourceToBsmlAttributes =
            new List<string>
            {
                "UIComponent",
                "UIObject"
            }.Select(s => new ClrTypeName($"{BsmlAttributesNamespace}.{s}")).ToList();

        public static readonly List<ClrTypeName> BsmlToSourceAttributes =
            new List<string>
            {
                "UIValue",
                "UIAction"
            }.Select(s => new ClrTypeName($"{BsmlAttributesNamespace}.{s}")).ToList();
    }
}