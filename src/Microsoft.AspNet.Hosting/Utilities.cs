using System;

namespace Microsoft.AspNet.Hosting
{
    internal static class Utilities
    {
        internal static Tuple<string, string> SplitTypeName(string identifier)
        {
            string typeName;
            string assemblyName;
            var parts = identifier.Split(new[] { ',' }, 2);
            if (parts.Length == 1)
            {
                typeName = null;
                assemblyName = identifier.Trim();
            }
            else if (parts.Length == 2)
            {
                typeName = parts[0].Trim();
                assemblyName = parts[1].Trim();
            }
            else
            {
                throw new ArgumentException("TODO: Unrecognized format", "identifier");
            }
            return new Tuple<string, string>(typeName, assemblyName);
        }
    }
}
