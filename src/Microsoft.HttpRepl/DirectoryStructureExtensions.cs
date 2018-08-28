// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.HttpRepl
{
    public static class DirectoryStructureExtensions
    {
        public static IEnumerable<string> GetDirectoryListingAtPath(this IDirectoryStructure structure, string path)
        {
            return structure.TraverseTo(path).DirectoryNames;
        }

        public static IDirectoryStructure TraverseTo(this IDirectoryStructure structure, string path)
        {
            string[] parts = path.Replace('\\', '/').Split('/');
            return structure.TraverseTo(parts);
        }

        public static IDirectoryStructure TraverseTo(this IDirectoryStructure structure, IEnumerable<string> pathParts)
        {
            IDirectoryStructure s = structure;
            IReadOnlyList<string> parts = pathParts.ToList();

            if (parts.Count == 0)
            {
                return s;
            }

            if (parts[0] == string.Empty && parts.Count > 1)
            {
                while (s.Parent != null)
                {
                    s = s.Parent;
                }
            }

            foreach (string part in parts)
            {
                if (part == ".")
                {
                    continue;
                }

                if (part == "..")
                {
                    s = s.Parent ?? s;
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    s = s.GetChildDirectory(part);
                }
            }

            return s;
        }
    }
}
