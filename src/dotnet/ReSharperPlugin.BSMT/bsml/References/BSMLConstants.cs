using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace ReSharperPlugin.BSMT_Rider.bsml
{
    public class BSMLConstants
    {
        public const string BsmlAttributesNamespace = "BeatSaberMarkupLanguage.Attributes";
        public const string BsmlVarPrefix = "~";
        public const string BsmlVarSuffix = "#";

        public static string StripPrefix(string str)
        {
            var containsPrefix = str.StartsWith(BsmlVarPrefix, StringComparison.OrdinalIgnoreCase);

            return !containsPrefix ? str : str[BsmlVarPrefix.Length..];
        }
        
        public static string StripSuffix(string str)
        {
            var containsSuffix = str.EndsWith(BsmlVarSuffix, StringComparison.OrdinalIgnoreCase);

            return !containsSuffix ? str : str[..BsmlVarSuffix.Length];
        }

        public static string StripGarbage(string str)
        {
            var containsPrefix = str.StartsWith(BsmlVarPrefix, StringComparison.OrdinalIgnoreCase);
            var containsSuffix = str.EndsWith(BsmlVarSuffix, StringComparison.OrdinalIgnoreCase);

            if (containsPrefix)
            {
                return containsSuffix ? str[BsmlVarPrefix.Length..BsmlVarSuffix.Length] : str[BsmlVarPrefix.Length..];
            }

            return containsSuffix ? str[..BsmlVarSuffix.Length] : str;
        }

        public static readonly ClrTypeName BsmlViewDefinitionAttribute =
            new($"{BsmlAttributesNamespace}.ViewDefinitionAttribute");

        public static readonly ClrTypeName BsmlViewControllerParent = new("BeatSaberMarkupLanguage.ViewControllers.BSMLViewController");

        public static readonly ClrTypeName BsmlUIValueAttribute = new($"{BsmlAttributesNamespace}.UIValue");

        public static readonly string BsmlCustomListTag = "custom-list"; // https://github.com/monkeymanboy/BeatSaberMarkupLanguage/blob/cb533a1955d39bba50049e18c546dc28601698ea/BeatSaberMarkupLanguage/Tags/CustomListTag.cs#L15
        

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

        public static bool IsClrTypeSourceToBsml(IClrTypeName other)
        {
            return SourceToBsmlAttributes.Any(a => Equals(a, other));
        }

        public static bool IsClrTypeBsmlToSource(IClrTypeName other)
        {
            return BsmlToSourceAttributes.Any(a => Equals(a, other));
        }
    }
}