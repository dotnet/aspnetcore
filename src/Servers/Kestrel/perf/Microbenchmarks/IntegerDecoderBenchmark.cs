// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.HPack;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class IntegerDecoderBenchmark
{
    private const int Iterations = 50_000;

    private readonly int _prefixLength = 5; // Arbitrary prefix length
    private readonly byte _singleByte = 0x1e; // 30
    private readonly byte[] _multiByte = new byte[] { 0x1f, 0xe0, 0xff, 0xff, 0xff, 0x03 }; // int32.MaxValue

    [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
    public void DecodeSingleByteInteger()
    {
        var integerDecoder = new IntegerDecoder();
        for (var i = 0; i < Iterations; i++)
        {
            integerDecoder.BeginTryDecode(_singleByte, _prefixLength, out _);
        }
    }

    [Benchmark(OperationsPerInvoke = Iterations)]
    public void DecodeMultiByteInteger()
    {
        var integerDecoder = new IntegerDecoder();
        for (var i = 0; i < Iterations; i++)
        {
            integerDecoder.BeginTryDecode(_multiByte[0], _prefixLength, out _);

            for (var j = 1; j < _multiByte.Length; j++)
            {
                integerDecoder.TryDecode(_multiByte[j], out _);
            }
        }
    }
}
