using System.Collections.Generic;
using System.Linq;

namespace RoutingSample.Web
{
    public static class DictionaryExtensions
    {
        public static string Print(this IDictionary<string, object> routeValues)
        {
            var values = routeValues.Select(kvp => kvp.Key + ":" + kvp.Value.ToString());

            return string.Join(" ", values);
        }
    }
}