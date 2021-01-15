// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// This comparer is optimized for use with dictionaries where the great majority of insertions/lookups
    /// don't match existing entries. For example, when building a dictionary of almost entirely unique keys.
    /// It's faster than the normal string comparer in this case because it doesn't use string.GetHashCode,
    /// and hence doesn't have to consider every character in the string.
    ///
    /// This primary scenario is <see cref="RenderTreeBuilder.ProcessDuplicateAttributes(int)"/>, which needs
    /// to detect when one attribute is overriding another, but in the vast majority of cases attributes don't
    /// actually override each other.
    /// </summary>
    internal class SimplifiedStringHashComparer : IEqualityComparer<string>
    {
        public readonly static SimplifiedStringHashComparer Instance = new SimplifiedStringHashComparer();

        public bool Equals(string? x, string? y)
        {
            return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string key)
        {
            var keyLength = key.Length;
            if (keyLength > 0)
            {
                // Consider just the length and middle and last characters.
                // This will produce a distinct result for a sufficiently large
                // proportion of attribute names.
                return unchecked(
                    char.ToLowerInvariant(key[keyLength - 1])
                    + 31 * char.ToLowerInvariant(key[keyLength / 2])
                    + 961 * keyLength);
            }
            else
            {
                return default;
            }
        }
    }
}
