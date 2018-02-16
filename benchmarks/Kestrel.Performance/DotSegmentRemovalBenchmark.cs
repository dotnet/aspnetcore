// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class DotSegmentRemovalBenchmark
    {
        // Immutable
        private const string _noDotSegments = "/long/request/target/for/benchmarking/what/else/can/we/put/here";
        private const string _singleDotSegments = "/long/./request/./target/./for/./benchmarking/./what/./else/./can/./we/./put/./here";
        private const string _doubleDotSegments = "/long/../request/../target/../for/../benchmarking/../what/../else/../can/../we/../put/../here";

        private readonly byte[] _noDotSegmentsAscii = Encoding.ASCII.GetBytes(_noDotSegments);
        private readonly byte[] _singleDotSegmentsAscii = Encoding.ASCII.GetBytes(_singleDotSegments);
        private readonly byte[] _doubleDotSegmentsAscii = Encoding.ASCII.GetBytes(_doubleDotSegments);

        private readonly byte[] _noDotSegmentsBytes = new byte[_noDotSegments.Length];
        private readonly byte[] _singleDotSegmentsBytes = new byte[_singleDotSegments.Length];
        private readonly byte[] _doubleDotSegmentsBytes = new byte[_doubleDotSegments.Length];

        [Benchmark(Baseline = true)]
        public unsafe int NoDotSegments()
        {
            _noDotSegmentsAscii.CopyTo(_noDotSegmentsBytes, 0);

            fixed (byte* start = _noDotSegmentsBytes)
            {
                return PathNormalizer.RemoveDotSegments(start, start + _noDotSegments.Length);
            }
        }

        [Benchmark]
        public unsafe int SingleDotSegments()
        {
            _singleDotSegmentsAscii.CopyTo(_singleDotSegmentsBytes, 0);

            fixed (byte* start = _singleDotSegmentsBytes)
            {
                return PathNormalizer.RemoveDotSegments(start, start + _singleDotSegments.Length);
            }
        }

        [Benchmark]
        public unsafe int DoubleDotSegments()
        {
            _doubleDotSegmentsAscii.CopyTo(_doubleDotSegmentsBytes, 0);

            fixed (byte* start = _doubleDotSegmentsBytes)
            {
                return PathNormalizer.RemoveDotSegments(start, start + _doubleDotSegments.Length);
            }
        }
    }
}
