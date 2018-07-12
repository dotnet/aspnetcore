// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal class DictionaryLookupJumpTable : JumpTable
    {
        private readonly int _defaultDestination;
        private readonly int _exitDestination;
        private readonly Dictionary<int, (string text, int destination)[]> _store;

        public DictionaryLookupJumpTable(
            int defaultDestination,
            int exitDestination,
            (string text, int destination)[] entries)
        {
            _defaultDestination = defaultDestination;
            _exitDestination = exitDestination;

            var map = new Dictionary<int, List<(string text, int destination)>>();

            for (var i = 0; i < entries.Length; i++)
            {
                var key = GetKey(entries[i].text.AsSpan());
                if (!map.TryGetValue(key, out var matches))
                {
                    matches = new List<(string text, int destination)>();
                    map.Add(key, matches);
                }

                matches.Add(entries[i]);
            }

            _store = map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }

        public override int GetDestination(string path, PathSegment segment)
        {
            if (segment.Length == 0)
            {
                return _exitDestination;
            }

            var key = GetKey(path.AsSpan(segment.Start, segment.Length));
            if (_store.TryGetValue(key, out var entries))
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var text = entries[i].text;
                    if (text.Length == segment.Length &&
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
            }

            return _defaultDestination;
        }

        private static int GetKey(string path, PathSegment segment)
        {
            return GetKey(path.AsSpan(segment.Start, segment.Length));
        }

        /// builds a key from the last byte of length + first 3 characters of text (converted to ascii)
        private static int GetKey(ReadOnlySpan<char> span)
        {
            var length = (byte)(span.Length & 0xFF);

            byte c0, c1, c2;
            switch (length)
            {
                case 0:
                    {
                        return 0;
                    }

                case 1:
                    {
                        c0 = (byte)(span[0] & 0x5F);
                        return (length << 24) | (c0 << 16);
                    }

                case 2:
                    {
                        c0 = (byte)(span[0] & 0x5F);
                        c1 = (byte)(span[1] & 0x5F);
                        return (length << 24) | (c0 << 16) | (c1 << 8);
                    }

                default:
                    {
                        c0 = (byte)(span[0] & 0x5F);
                        c1 = (byte)(span[1] & 0x5F);
                        c2 = (byte)(span[2] & 0x5F);
                        return (length << 24) | (c0 << 16) | (c1 << 8) | c2;
                    }
            }
        }
    }
}
