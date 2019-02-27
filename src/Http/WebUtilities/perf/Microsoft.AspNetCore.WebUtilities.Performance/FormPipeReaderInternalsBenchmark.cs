// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.WebUtilities.Performance
{
    /// <summary>
    /// Test internal parsing speed of FormPipeReader without pipe
    /// </summary>
    public class FormPipeReaderInternalsBenchmark
    {
        private ReadOnlySequence<byte> _singleSegmentUtf8ReadOnlySequence;
        private ReadOnlySequence<byte> _multiSegmentUtf8ReadOnlySequence;
        private ReadOnlySequence<byte> _singleSegmentUnicodeReadOnlySequence;
        private ReadOnlySequence<byte> _multiSegmentUnicodeReadOnlySequence;
        private KeyValueAccumulator _accumulator;
        private FormPipeReader _formPipeReader;

        [IterationSetup]
        public void Setup()
        {
            _singleSegmentUtf8ReadOnlySequence = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("foo=bar&baz=boo"));
            _multiSegmentUtf8ReadOnlySequence = ReadOnlySequenceFactory.CreateSegments(Encoding.UTF8.GetBytes("foo=bar&baz=boo"), Encoding.UTF8.GetBytes("&haha=hehe&lol=temp"));
            _singleSegmentUnicodeReadOnlySequence = new ReadOnlySequence<byte>(Encoding.Unicode.GetBytes("foo=bar&baz=boo"));
            _multiSegmentUnicodeReadOnlySequence = ReadOnlySequenceFactory.CreateSegments(Encoding.Unicode.GetBytes("foo=bar&baz=boo"), Encoding.Unicode.GetBytes("&haha=hehe&lol=temp"));
            _formPipeReader = new FormPipeReader(null, encoding: Encoding.UTF8);

            _accumulator = default;
        }

        [Benchmark]
        public void ReadUtf8Data()
        {
            _formPipeReader.ParseFormValues(ref _singleSegmentUtf8ReadOnlySequence, ref _accumulator, isFinalBlock: true);
        }

        [Benchmark]
        public void ReadUnicodeData()
        {
            _formPipeReader.ParseFormValues(ref _singleSegmentUnicodeReadOnlySequence, ref _accumulator, isFinalBlock: true);
        }

        [Benchmark]
        public void ReadUtf8MultipleBlockData()
        {
            _formPipeReader.ParseFormValues(ref _multiSegmentUtf8ReadOnlySequence, ref _accumulator, isFinalBlock: true);
        }

        [Benchmark]
        public void ReadUnicodeMultipleBlockData()
        {
            _formPipeReader.ParseFormValues(ref _multiSegmentUnicodeReadOnlySequence, ref _accumulator, isFinalBlock: true);
        }
    }
}
