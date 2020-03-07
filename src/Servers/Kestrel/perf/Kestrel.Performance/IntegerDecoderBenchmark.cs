// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.HPack;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class IntegerDecoderBenchmark
    {
        private const int Iterations = 50_000;

        private int _prefixLength = 5; // Arbitrary prefix length
        private byte _singleByte = 0x1e; // 30
        private byte[] _multiByte = new byte[] { 0x1f, 0xe0, 0xff, 0xff, 0xff, 0x03}; // int32.MaxValue
        private IntegerDecoder _integerDecoder = new IntegerDecoder();

        [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
        public void DecodeSingleByteInteger()
        {
            for (var i = 0; i < Iterations; i++)
            {
                _integerDecoder.BeginTryDecode(_singleByte, _prefixLength, out _);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void DecodeMultiByteInteger()
        {
            for (var i = 0; i < Iterations; i++)
            {
                _integerDecoder.BeginTryDecode(_multiByte[0], _prefixLength, out _);

                for (var j = 1; j < _multiByte.Length; j++)
                {
                    _integerDecoder.TryDecode(_multiByte[j], out _);
                }
            }
        }
    }
}
