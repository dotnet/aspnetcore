// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class JumpTableMultipleEntryBenchmark
    {
        private string[] _strings;
        private PathSegment[] _segments;

        private JumpTable _linearSearch;
        private JumpTable _dictionary;
        private JumpTable _trie;
        private JumpTable _vectorTrie;

        // All factors of 100 to support sampling
        [Params(2, 5, 10, 25, 50, 100)]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            _strings = GetStrings(100);
            _segments = new PathSegment[100];

            for (var i = 0; i < _strings.Length; i++)
            {
                _segments[i] = new PathSegment(0, _strings[i].Length);
            }

            var samples = new int[Count];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = i * (_strings.Length / Count);
            }

            var entries = new List<(string text, int _)>();
            for (var i = 0; i < samples.Length; i++)
            {
                entries.Add((_strings[samples[i]], i));
            }

            _linearSearch = new LinearSearchJumpTable(0, -1, entries.ToArray());
            _dictionary = new DictionaryJumpTable(0, -1, entries.ToArray());
            _trie = new ILEmitTrieJumpTable(0, -1, entries.ToArray(), vectorize: false, _dictionary);
            _vectorTrie = new ILEmitTrieJumpTable(0, -1, entries.ToArray(), vectorize: true, _dictionary);
        }

        // This baseline is similar to SingleEntryJumpTable. We just want
        // something stable to compare against.
        [Benchmark(Baseline = true, OperationsPerInvoke = 100)]
        public int Baseline()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                var @string = strings[i];
                var segment = segments[i];

                if (segment.Length == 0)
                {
                    destination = -1;
                }
                else if (segment.Length != @string.Length)
                {
                    destination = 1;
                }
                else
                {
                    destination = string.Compare(
                        @string,
                        segment.Start,
                        @string,
                        0,
                        segment.Length,
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public int LinearSearch()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                destination = _linearSearch.GetDestination(strings[i], segments[i]);
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public int Dictionary()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                destination = _dictionary.GetDestination(strings[i], segments[i]);
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public int Trie()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                destination = _trie.GetDestination(strings[i], segments[i]);
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public int VectorTrie()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                destination = _vectorTrie.GetDestination(strings[i], segments[i]);
            }

            return destination;
        }

        private static string[] GetStrings(int count)
        {
            var strings = new string[count];
            for (var i = 0; i < count; i++)
            {
                var guid = Guid.NewGuid().ToString();

                // Between 5 and 36 characters
                var text = guid.Substring(0, Math.Max(5, Math.Min(i, 36)));
                if (char.IsDigit(text[0]))
                {
                    // Convert first character to a letter.
                    text = ((char)(text[0] + ('G' - '0'))) + text.Substring(1);
                }

                if (i % 2 == 0)
                {
                    // Lowercase half of them
                    text = text.ToLowerInvariant();
                }

                strings[i] = text;
            }

            return strings;
        }
    }
}
