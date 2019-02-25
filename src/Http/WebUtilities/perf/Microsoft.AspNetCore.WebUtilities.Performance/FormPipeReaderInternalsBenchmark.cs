// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.WebUtilities.Performance
{
    public class FormPipeReaderInternalsBenchmark
    {
        private ReadOnlySequence<byte> _singleSegmentReadOnlySequence;
        private FormPipeReader _formPipeReader;
        private KeyValueAccumulator _accumulator;

        //private ReadOnlySequence<byte> _multiSegmentReadOnlySequence;

        [IterationSetup]
        public void Setup()
        {
            _singleSegmentReadOnlySequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("Hello world"));
            _formPipeReader = new FormPipeReader(null);
            _accumulator = default;
        }

        [Benchmark]
        public void ReadSmallFormAsync()
        {
            _formPipeReader.TryParseFormValues(ref _singleSegmentReadOnlySequence, ref _accumulator, false);
        }
    }
}
