// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoTasks.Utilities
{
    internal static class MSBuildListSplitter
    {
        private static readonly char[] SemiColon = { ';' };

        public static IEnumerable<string> SplitItemList(string value)
        {
            return string.IsNullOrEmpty(value)
                ? Enumerable.Empty<string>()
                : value.Split(SemiColon, StringSplitOptions.RemoveEmptyEntries);
        }

        public static Dictionary<string, string> GetNamedProperties(string input)
        {
            var values = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(input))
            {
                return values;
            }

            foreach (var item in input.Split(SemiColon, StringSplitOptions.RemoveEmptyEntries))
            {
                var splitIdx = item.IndexOf('=');
                if (splitIdx <= 0)
                {
                    continue;
                }

                var key = item.Substring(0, splitIdx).Trim();
                var value = item.Substring(splitIdx + 1);
                values[key] = value;
            }

            return values;
        }
    }
}
