using System;

namespace Microsoft.AspNet.Hosting
{
    internal static class Utilities
    {
        internal static Tuple<string, string> SplitTypeName(string identifier)
        {
            string typeName = null;
            string assemblyName = identifier.Trim();
            var parts = identifier.Split(new[] { ',' }, 2);
            if (parts.Length == 2)
            {
                typeName = parts[0].Trim();
                assemblyName = parts[1].Trim();
            }
            return new Tuple<string, string>(typeName, assemblyName);
        }
    }
}
