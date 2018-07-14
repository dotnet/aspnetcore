// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // An optimized jump table that trades a small amount of additional memory for
    // hash-table like performance.
    //
    // The optimization here is to use the first character of the known entries
    // as a 'key' in the hash table in the space of A-Z. This gives us a maximum
    // of 26 buckets (hence the reduced memory)
    internal class AsciiKeyedJumpTable : JumpTable
    {
        public static bool TryCreate(
            int defaultDestination,
            int exitDestination,
            List<(string text, int destination)> entries,
            out JumpTable result)
        {
            result = null;

            // First we group string by their uppercase letter. If we see a string
            // that starts with a non-ASCII letter 
            var map = new Dictionary<char, List<(string text, int destination)>>();

            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].text.Length == 0)
                {
                    return false;
                }

                if (!IsAscii(entries[i].text))
                {
                    return false;
                }

                var first = ToUpperAscii(entries[i].text[0]);
                if (first < 'A' || first > 'Z')
                {
                    // Not a letter
                    return false;
                }

                if (!map.TryGetValue(first, out var matches))
                {
                    matches = new List<(string text, int destination)>();
                    map.Add(first, matches);
                }

                matches.Add(entries[i]);
            }

            var next = 0;
            var ordered = new(string text, int destination)[entries.Count];
            var indexes = new int[26 * 2];
            for (var i = 0; i < 26; i++)
            {
                indexes[i * 2] = next;

                var length = 0;
                if (map.TryGetValue((char)('A' + i), out var matches))
                {
                    length += matches.Count;
                    for (var j = 0; j < matches.Count; j++)
                    {
                        ordered[next++] = matches[j];
                    }
                }

                indexes[i * 2 + 1] = length;
            }

            result = new AsciiKeyedJumpTable(defaultDestination, exitDestination, ordered, indexes);
            return true;
        }

        private readonly int _defaultDestination;
        private readonly int _exitDestination;
        private readonly (string text, int destination)[] _entries;
        private readonly int[] _indexes;

        private AsciiKeyedJumpTable(
            int defaultDestination,
            int exitDestination,
            (string text, int destination)[] entries,
            int[] indexes)
        {
            _defaultDestination = defaultDestination;
            _exitDestination = exitDestination;
            _entries = entries;
            _indexes = indexes;
        }

        public override unsafe int GetDestination(string path, PathSegment segment)
        {
            if (segment.Length == 0)
            {
                return _exitDestination;
            }

            var c = path[segment.Start];
            if (!IsAscii(c))
            {
                return _defaultDestination;
            }

            c = ToUpperAscii(c);
            if (c < 'A' || c > 'Z')
            {
                // Character is non-ASCII or not a letter. Since we know that all of the entries are ASCII
                // and begin with a letter this is not a match.
                return _defaultDestination;
            }

            var offset = (c - 'A') * 2;
            var start = _indexes[offset];
            var length = _indexes[offset + 1];

            var entries = _entries;
            for (var i = start; i < start + length; i++)
            {
                var text = entries[i].text;
                if (segment.Length == text.Length &&
                    string.Compare(
                        path,
                        segment.Start,
                        text,
                        0,
                        segment.Length,
                        StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return entries[i].destination;
                }
            }

            return _defaultDestination;
        }

        internal static bool IsAscii(char c)
        {
            // ~0x7F is a bit mask that checks for bits that won't be set in an ASCII character.
            // ASCII only uses the lowest 7 bits.
            return (c & ~0x7F) == 0;
        }

        internal static bool IsAscii(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (!IsAscii(c))
                {
                    return false;
                }
            }

            return true;
        }

        internal static char ToUpperAscii(char c)
        {
            // 0x5F can be used to convert a character to uppercase ascii (assuming it's a letter).
            // This works because lowercase ASCII chars are exactly 32 less than their uppercase
            // counterparts.
            return (char)(c & 0x5F);
        }
    }
}