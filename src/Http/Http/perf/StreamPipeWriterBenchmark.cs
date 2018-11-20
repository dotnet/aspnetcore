// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http
{
    public class StreamPipeWriterBenchmark
    {
        private Stream _memoryStream;
        private StreamPipeWriter _pipeWriter;
        private static byte[] _helloWorldBytes = Encoding.ASCII.GetBytes("Hello World");
        private static byte[] _largeWrite = Encoding.ASCII.GetBytes(new string('a', 50000));

        [IterationSetup]
        public void Setup()
        {
            _memoryStream = new NoopStream();
            _pipeWriter = new StreamPipeWriter(_memoryStream);
        }

        [Benchmark]
        public async Task WriteHelloWorld()
        {
            await _pipeWriter.WriteAsync(_helloWorldBytes);
        }

        [Benchmark]
        public async Task WriteHelloWorldLargeWrite()
        {
            await _pipeWriter.WriteAsync(_largeWrite);
        }
    }
}
