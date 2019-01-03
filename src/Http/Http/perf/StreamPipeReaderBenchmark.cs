// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http
{
    public class StreamPipeReaderBenchmark
    {
        private StreamPipeReader _pipeReaderNoop;
        private StreamPipeReader _pipeReaderHelloWorld;
        private StreamPipeReader _pipeReaderHelloWorldAync;

        [IterationSetup]
        public void Setup()
        {
            _pipeReaderNoop = new StreamPipeReader(new NoopStream());
            _pipeReaderHelloWorld = new StreamPipeReader(new HelloWorldStream());
            _pipeReaderHelloWorldAync = new StreamPipeReader(new HelloWorldAsyncStream());
        }

        [Benchmark]
        public async Task ReadNoop()
        {
            await _pipeReaderNoop.ReadAsync();
        }

        [Benchmark]
        public async Task ReadHelloWorld()
        {
            var result = await _pipeReaderHelloWorld.ReadAsync();
            _pipeReaderHelloWorld.AdvanceTo(result.Buffer.End);
        }

        [Benchmark]
        public async Task ReadHelloWorldAsync()
        {
            var result = await _pipeReaderHelloWorldAync.ReadAsync();
            _pipeReaderHelloWorldAync.AdvanceTo(result.Buffer.End);
        }

        private class HelloWorldStream : NoopStream
        {
            private static byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                bytes.CopyTo(buffer, 0);
                return Task.FromResult(11);
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                bytes.CopyTo(buffer);

                return new ValueTask<int>(11);
            }
        }

        private class HelloWorldAsyncStream : NoopStream
        {
            private static byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await Task.Yield();
                bytes.CopyTo(buffer, 0);
                return 11;
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                bytes.CopyTo(buffer);
                return 11;
            }
        }
    }
}
