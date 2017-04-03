// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [Config(typeof(CoreConfig))]
    public class FrameWritingBenchmark
    {
        private readonly TestFrame<object> _frame;
        private readonly TestFrame<object> _frameChunked;
        private readonly byte[] _writeData;

        public FrameWritingBenchmark()
        {
            _frame = MakeFrame();
            _frameChunked = MakeFrame();
            _writeData = new byte[1];
        }

        [Setup]
        public void Setup()
        {
            _frame.Reset();
            _frame.RequestHeaders.Add("Content-Length", "1073741824");

            _frameChunked.Reset();
            _frameChunked.RequestHeaders.Add("Transfer-Encoding", "chunked");
        }

        [Benchmark]
        public void Write()
        {
            _frame.Write(new ArraySegment<byte>(_writeData));
        }

        [Benchmark]
        public void WriteChunked()
        {
            _frameChunked.Write(new ArraySegment<byte>(_writeData));
        }

        [Benchmark]
        public async Task WriteAsync()
        {
            await _frame.WriteAsync(new ArraySegment<byte>(_writeData), default(CancellationToken));
        }

        [Benchmark]
        public async Task WriteAsyncChunked()
        {
            await _frameChunked.WriteAsync(new ArraySegment<byte>(_writeData), default(CancellationToken));
        }

        [Benchmark]
        public async Task WriteAsyncAwaited()
        {
            await _frame.WriteAsyncAwaited(new ArraySegment<byte>(_writeData), default(CancellationToken));
        }

        [Benchmark]
        public async Task WriteAsyncAwaitedChunked()
        {
            await _frameChunked.WriteAsyncAwaited(new ArraySegment<byte>(_writeData), default(CancellationToken));
        }

        [Benchmark]
        public async Task ProduceEnd()
        {
            await _frame.ProduceEndAsync();
        }

        [Benchmark]
        public async Task ProduceEndChunked()
        {
            await _frameChunked.ProduceEndAsync();
        }

        private TestFrame<object> MakeFrame()
        {
            var socketInput = new PipeFactory().Create();

            var serviceContext = new ServiceContext
            {
                DateHeaderValueManager = new DateHeaderValueManager(),
                ServerOptions = new KestrelServerOptions(),
                Log = new MockTrace(),
                HttpParserFactory = f => new KestrelHttpParser(log: null)
            };
            var frameContext = new FrameContext
            {
                ServiceContext = serviceContext,
                ConnectionInformation = new MockConnectionInformation()
            };

            var frame = new TestFrame<object>(application: null, context: frameContext)
            {
                Input = socketInput.Reader,
                Output = new MockSocketOutput()
            };

            frame.Reset();
            frame.InitializeHeaders();

            return frame;
        }
    }
}
