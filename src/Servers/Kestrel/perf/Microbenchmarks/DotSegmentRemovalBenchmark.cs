// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class DotSegmentRemovalBenchmark
{
    // Immutable
    private const string _noDotSegments = "/long/request/target/for/benchmarking/what/else/can/we/put/here";
    private const string _singleDotSegments = "/long/./request/./target/./for/./benchmarking/./what/./else/./can/./we/./put/./here";
    private const string _doubleDotSegments = "/long/../request/../target/../for/../benchmarking/../what/../else/../can/../we/../put/../here";
    private const string _oneSingleDotSegments = "/long/request/target/for/./benchmarking/what/else/can/we/put/here";
    private const string _oneDoubleDotSegments = "/long/request/target/for/../benchmarking/what/else/can/we/put/here";

    private readonly byte[] _noDotSegmentsAscii = Encoding.ASCII.GetBytes(_noDotSegments);
    private readonly byte[] _singleDotSegmentsAscii = Encoding.ASCII.GetBytes(_singleDotSegments);
    private readonly byte[] _doubleDotSegmentsAscii = Encoding.ASCII.GetBytes(_doubleDotSegments);
    private readonly byte[] _oneDingleDotSegmentsAscii = Encoding.ASCII.GetBytes(_oneSingleDotSegments);
    private readonly byte[] _oneDoubleDotSegmentsAscii = Encoding.ASCII.GetBytes(_oneDoubleDotSegments);

    private readonly byte[] _noDotSegmentsBytes = new byte[_noDotSegments.Length];
    private readonly byte[] _singleDotSegmentsBytes = new byte[_singleDotSegments.Length];
    private readonly byte[] _doubleDotSegmentsBytes = new byte[_doubleDotSegments.Length];
    private readonly byte[] _oneSingleDotSegmentsBytes = new byte[_singleDotSegments.Length];
    private readonly byte[] _oneDoubleDotSegmentsBytes = new byte[_doubleDotSegments.Length];

    [Benchmark(Baseline = true)]
    public int NoDotSegments()
    {
        _noDotSegmentsAscii.CopyTo(_noDotSegmentsBytes, 0);
        return PathNormalizer.RemoveDotSegments(_noDotSegmentsBytes);
    }

    [Benchmark]
    public int SingleDotSegments()
    {
        _singleDotSegmentsAscii.CopyTo(_singleDotSegmentsBytes, 0);
        return PathNormalizer.RemoveDotSegments(_singleDotSegmentsBytes);
    }

    [Benchmark]
    public int DoubleDotSegments()
    {
        _doubleDotSegmentsAscii.CopyTo(_doubleDotSegmentsBytes, 0);
        return PathNormalizer.RemoveDotSegments(_doubleDotSegmentsBytes);
    }

    [Benchmark]
    public int OneSingleDotSegments()
    {
        _oneDingleDotSegmentsAscii.CopyTo(_oneSingleDotSegmentsBytes, 0);
        return PathNormalizer.RemoveDotSegments(_oneSingleDotSegmentsBytes);
    }

    [Benchmark]
    public int OneDoubleDotSegments()
    {
        _oneDoubleDotSegmentsAscii.CopyTo(_oneDoubleDotSegmentsBytes, 0);
        return PathNormalizer.RemoveDotSegments(_oneDoubleDotSegmentsBytes);
    }
}
