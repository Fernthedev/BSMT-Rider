using System;

namespace ReSharperPlugin.BSMT_Rider.utils
{
    public static class StringUtils
    {
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace)
        {
            var place = Source.LastIndexOf(Find, StringComparison.Ordinal);

            if(place == -1)
                return Source;

            var result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
    }
}