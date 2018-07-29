using System.Collections.Generic;

namespace Microsoft.Repl
{
    public static class Utils
    {
        public static string Stringify(this IReadOnlyList<char> keys)
        {
            return string.Join("", keys);
        }
    }
}
