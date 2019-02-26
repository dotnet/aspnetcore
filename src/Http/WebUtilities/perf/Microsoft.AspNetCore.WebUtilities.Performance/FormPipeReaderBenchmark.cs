// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormPipeReaderBenchmark
    {
        private byte[] _smallBytes;
        private byte[] _largeBytes;

        [GlobalSetup]
        public void Setup()
        {
            _smallBytes = Encoding.UTF8.GetBytes("foo=bar&baz=boo");
            _largeBytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E=%22%25%2D%2E%3C%3E%5C%5E%5F%60%7B%7C%7D%7E&", 1000)) + "foo=bar");
        }

        [Benchmark]
        public async Task CreatePipe()
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(_smallBytes);
            pipe.Writer.Complete();
            var formReader = new FormPipeReader(pipe.Reader);
        }

        [Benchmark]
        public async Task ReadSmallFormAsync()
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(_smallBytes);
            pipe.Writer.Complete();
            var formReader = new FormPipeReader(pipe.Reader);
            await formReader.ReadFormAsync();
        }

        [Benchmark]
        public async Task CreateLargePipe()
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(_largeBytes);
            pipe.Writer.Complete();
            var formReader = new FormPipeReader(pipe.Reader);
            formReader.ValueCountLimit = 5000;
        }

        [Benchmark]
        public async Task ReadLargeFormAsync()
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(_largeBytes);
            pipe.Writer.Complete();
            var formReader = new FormPipeReader(pipe.Reader);
            formReader.ValueCountLimit = 5000;

            await formReader.ReadFormAsync();
        }
    }
}
