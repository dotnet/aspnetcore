// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Antiforgery.Microbenchmarks.Benchmarks;

[AspNetCoreBenchmark]
public class SevenBitEncodedLengthBenchmark
{
    private static readonly ushort[] EncodedLengths = CreateEncodedLengths();
    private readonly byte[] _destination = new byte[2];

    [Params(0, 1, 127, 128, 255)]
    public uint Value { get; set; }

    [Benchmark(Baseline = true)]
    public int Current()
        => Write7BitEncodedInt(_destination, Value);

    [Benchmark]
    public int Lookup()
    {
        var encoded = EncodedLengths[Value];
        _destination[0] = (byte)encoded;

        if (Value < 128)
        {
            return 1;
        }

        _destination[1] = (byte)(encoded >> 8);
        return 2;
    }

    private static ushort[] CreateEncodedLengths()
    {
        var lookup = new ushort[256];
        for (uint value = 0; value < lookup.Length; value++)
        {
            lookup[value] = value < 128
                ? (ushort)value
                : (ushort)((value | 0x80) | ((value >> 7) << 8));
        }

        return lookup;
    }

    private static int Write7BitEncodedInt(Span<byte> target, uint value)
    {
        var index = 0;
        while (value > 0x7Fu)
        {
            target[index++] = (byte)(value | ~0x7Fu);
            value >>= 7;
        }

        target[index++] = (byte)value;
        return index;
    }
}
