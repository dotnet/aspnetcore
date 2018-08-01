// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.HttpRepl.Commands
{
    public class Formatter
    {
        private int _prefix;
        private int _maxDepth;

        public void RegisterEntry(int prefixLength, int depth)
        {
            if (depth > _maxDepth)
            {
                _maxDepth = depth;
            }

            if (prefixLength > _prefix)
            {
                _prefix = prefixLength;
            }
        }

        public string Format(string prefix, string entry, int level)
        {
            string indent = "".PadRight(level * 4);
            return (indent + prefix).PadRight(_prefix + 3 + _maxDepth * 4) + entry;
        }
    }
}
