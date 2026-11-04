// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing.Matching;

public class JumpTableZeroEntryBenchmark
{
    private JumpTable _table;
    private string[] _strings;
    private PathSegment[] _segments;

    [GlobalSetup]
    public void Setup()
    {
        _table = new ZeroEntryJumpTable(0, -1);
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

        var destination = 0;
        for (var i = 0; i < strings.Length; i++)
        {
            destination = segments[i].Length == 0 ? -1 : 0;
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
            destination = _table.GetDestination(strings[i], segments[i]);
        }

        return destination;
    }
}
