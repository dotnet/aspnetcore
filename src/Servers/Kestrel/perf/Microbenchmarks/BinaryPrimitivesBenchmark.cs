// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class BinaryPrimitivesBenchmark
{
    private const int Iterations = 100;

    private byte[] _data;

    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[4];
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
    public uint GetUInt32AsBitwise()
    {
        var v = 0u;
        for (int i = 0; i < 1_000_000; i++)
        {
            v = (uint)((_data[0] << 24) | (_data[1] << 16) | (_data[2] << 8) | _data[3]);
        }
        return v;
    }

    [Benchmark(OperationsPerInvoke = Iterations)]
    public unsafe uint GetUInt32AsBinary()
    {
        var v = 0u;
        for (int i = 0; i < 1_000_000; i++)
        {
            v = BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan());
        }
        return v;
    }
}
