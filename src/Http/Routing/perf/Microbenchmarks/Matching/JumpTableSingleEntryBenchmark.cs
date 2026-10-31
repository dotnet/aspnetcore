// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching;

public class JumpTableSingleEntryBenchmark
{
    private JumpTable _default;
    private JumpTable _trie;
    private JumpTable _vectorTrie;
    private JumpTable _ascii;

    private string[] _strings;
    private PathSegment[] _segments;

    [GlobalSetup]
    public void Setup()
    {
        _default = new SingleEntryJumpTable(0, -1, "hello-world", 1);
        _trie = new ILEmitTrieJumpTable(0, -1, new[] { ("hello-world", 1), }, vectorize: false, _default);
        _vectorTrie = new ILEmitTrieJumpTable(0, -1, new[] { ("hello-world", 1), }, vectorize: true, _default);
        _ascii = new SingleEntryAsciiJumpTable(0, -1, "hello-world", 1);

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
    public int Default()
    {
        var strings = _strings;
        var segments = _segments;

        var destination = 0;
        for (var i = 0; i < strings.Length; i++)
        {
            destination = _default.GetDestination(strings[i], segments[i]);
        }

        return destination;
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public int Ascii()
    {
        var strings = _strings;
        var segments = _segments;

        var destination = 0;
        for (var i = 0; i < strings.Length; i++)
        {
            destination = _ascii.GetDestination(strings[i], segments[i]);
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
}
