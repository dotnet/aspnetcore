// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class JumpTableSingleEntryBenchmark
    {
        private JumpTable _implementation;
        private JumpTable _prototype;
        private JumpTable _trie;
        private JumpTable _vectorTrie;

        private string[] _strings;
        private PathSegment[] _segments;

        [GlobalSetup]
        public void Setup()
        {
            _implementation = new SingleEntryJumpTable(0, -1, "hello-world", 1);
            _prototype = new SingleEntryAsciiVectorizedJumpTable(0, -2, "hello-world", 1);
            _trie = new ILEmitTrieJumpTable(0, -1, new[] { ("hello-world", 1), }, vectorize: false, _implementation);
            _vectorTrie = new ILEmitTrieJumpTable(0, -1, new[] { ("hello-world", 1), }, vectorize: true, _implementation);

            _strings = new string[]
            {
                "index/foo/2",
                "index/hello-world1/2",
                "index/hello-world/2",
                "index//2",
                "index/hillo-goodbye/2",
            };
            _segments = new PathSegment[]
            {
                new PathSegment(6, 3),
                new PathSegment(6, 12),
                new PathSegment(6, 11),
                new PathSegment(6, 0),
                new PathSegment(6, 13),
            };
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = 5)]
        public int Baseline()
        {
            var strings = _strings;
            var segments = _segments;

            int destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                var @string = strings[i];
                var segment = segments[i];

                if (segment.Length == 0)
                {
                    destination = -1;
                }
                else if (segment.Length != "hello-world".Length)
                {
                    destination = 1;
                }
                else
                {
                    destination = string.Compare(
                        @string,
                        segment.Start,
                        "hello-world",
                        0,
                        segment.Length,
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 5)]
        public int Implementation()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                destination = _implementation.GetDestination(strings[i], segments[i]);
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 5)]
        public int Prototype()
        {
            var strings = _strings;
            var segments = _segments;

            var destination = 0;
            for (var i = 0; i < strings.Length; i++)
            {
                destination = _prototype.GetDestination(strings[i], segments[i]);
            }

            return destination;
        }

        [Benchmark(OperationsPerInvoke = 5)]
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

        [Benchmark(OperationsPerInvoke = 5)]
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

        private class SingleEntryAsciiVectorizedJumpTable : JumpTable
        {
            private readonly int _defaultDestination;
            private readonly int _exitDestination;
            private readonly string _text;
            private readonly int _destination;

            private readonly ulong[] _values;
            private readonly int _residue0Lower;
            private readonly int _residue0Upper;
            private readonly int _residue1Lower;
            private readonly int _residue1Upper;
            private readonly int _residue2Lower;
            private readonly int _residue2Upper;

            public SingleEntryAsciiVectorizedJumpTable(
                int defaultDestination,
                int exitDestination,
                string text,
                int destination)
            {
                _defaultDestination = defaultDestination;
                _exitDestination = exitDestination;
                _text = text;
                _destination = destination;

                var length = text.Length;
                var span = text.ToLowerInvariant().AsSpan();
                ref var p = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span));

                _values = new ulong[length / 4];
                for (var i = 0; i < length / 4; i++)
                {
                    _values[i] = Unsafe.ReadUnaligned<ulong>(ref p);
                    p = Unsafe.Add(ref p, 64);
                }
                switch (length % 4)
                {
                    case 1:
                        {
                            var c = Unsafe.ReadUnaligned<char>(ref p);
                            _residue0Lower = char.ToLowerInvariant(c);
                            _residue0Upper = char.ToUpperInvariant(c);

                            break;
                        }

                    case 2:
                        {
                            var c = Unsafe.ReadUnaligned<char>(ref p);
                            _residue0Lower = char.ToLowerInvariant(c);
                            _residue0Upper = char.ToUpperInvariant(c);

                            p = Unsafe.Add(ref p, 2);
                            c = Unsafe.ReadUnaligned<char>(ref p);
                            _residue1Lower = char.ToLowerInvariant(c);
                            _residue1Upper = char.ToUpperInvariant(c);

                            break;
                        }

                    case 3:
                        {
                            var c = Unsafe.ReadUnaligned<char>(ref p);
                            _residue0Lower = char.ToLowerInvariant(c);
                            _residue0Upper = char.ToUpperInvariant(c);

                            p = Unsafe.Add(ref p, 2);
                            c = Unsafe.ReadUnaligned<char>(ref p);
                            _residue1Lower = char.ToLowerInvariant(c);
                            _residue1Upper = char.ToUpperInvariant(c);

                            p = Unsafe.Add(ref p, 2);
                            c = Unsafe.ReadUnaligned<char>(ref p);
                            _residue2Lower = char.ToLowerInvariant(c);
                            _residue2Upper = char.ToUpperInvariant(c);

                            break;
                        }
                }
            }

            public override int GetDestination(string path, PathSegment segment)
            {
                var length = segment.Length;
                var span = path.AsSpan(segment.Start, length);
                ref var p = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span));

                var i = 0;
                while (length > 3)
                {
                    var value = Unsafe.ReadUnaligned<ulong>(ref p);

                    if ((value & ~0x007F007F007F007FUL) == 0)
                    {
                        return _defaultDestination;
                    }

                    var ulongLowerIndicator = value + (0x0080008000800080UL - 0x0041004100410041UL);
                    var ulongUpperIndicator = value + (0x0080008000800080UL - 0x005B005B005B005BUL);
                    var ulongCombinedIndicator = (ulongLowerIndicator ^ ulongUpperIndicator) & 0x0080008000800080UL;
                    var mask = (ulongCombinedIndicator) >> 2;

                    value ^= mask;

                    if (value != _values[i])
                    {
                        return _defaultDestination;
                    }

                    i++;
                    length -= 4;
                    p = ref Unsafe.Add(ref p, 64);
                }

                switch (length)
                {
                    case 1:
                        {
                            var c = Unsafe.ReadUnaligned<char>(ref p);
                            if (c != _residue0Lower && c != _residue0Upper)
                            {
                                return _defaultDestination;
                            }

                            break;
                        }

                    case 2:
                        {
                            var c = Unsafe.ReadUnaligned<char>(ref p);
                            if (c != _residue0Lower && c != _residue0Upper)
                            {
                                return _defaultDestination;
                            }

                            p = ref Unsafe.Add(ref p, 2);
                            c = Unsafe.ReadUnaligned<char>(ref p);
                            if (c != _residue1Lower && c != _residue1Upper)
                            {
                                return _defaultDestination;
                            }

                            break;
                        }

                    case 3:
                        {
                            var c = Unsafe.ReadUnaligned<char>(ref p);
                            if (c != _residue0Lower && c != _residue0Upper)
                            {
                                return _defaultDestination;
                            }

                            p = ref Unsafe.Add(ref p, 2);
                            c = Unsafe.ReadUnaligned<char>(ref p);
                            if (c != _residue1Lower && c != _residue1Upper)
                            {
                                return _defaultDestination;
                            }

                            p = ref Unsafe.Add(ref p, 2);
                            c = Unsafe.ReadUnaligned<char>(ref p);
                            if (c != _residue2Lower && c != _residue2Upper)
                            {
                                return _defaultDestination;
                            }

                            break;
                        }
                }

                return _destination;
            }
        }
    }
}
