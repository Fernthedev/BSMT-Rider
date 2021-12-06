using System;
using System.Collections.Generic;
using System.Text;

namespace ReSharperPlugin.BSMT_Rider.utils
{
    public static class IEnumerableExtensions
    {
        public static string ToStringList(this IEnumerable<object> objects)
        {
            return ToStringList<object>(objects);
        }
        
        public static string ToStringList<T>(this IEnumerable<T> objects)
        {
            StringBuilder builder = new("{");
            foreach (var o in objects)
            {
                builder.Append($"{o}, ");
            }

            builder.Append("}");

            return builder.ToString();
        }
        
        public static string ToStringList(this IEnumerable<string> objects)
        {
            StringBuilder builder = new("{");
            foreach (var o in objects)
            {
                builder.Append($"{o}, ");
            }

            builder.Append("}");

            return builder.ToString();
        }
        
        public static string ToStringList<T>(this IEnumerable<T> objects, Func<T, string> toString)
        {
            StringBuilder builder = new("{");
            foreach (var o in objects)
            {
                builder.Append($"{toString(o)}, ");
            }

            builder.Append("}");

            return builder.ToString();
        }
    }
}