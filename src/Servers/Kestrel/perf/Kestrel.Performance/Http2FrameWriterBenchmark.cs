// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class Http2FrameWriterBenchmark
    {
        private MemoryPool<byte> _memoryPool;
        private Pipe _pipe;
        private Http2FrameWriter _frameWriter;
        private HttpResponseHeaders _responseHeaders;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _memoryPool = SlabMemoryPoolFactory.Create();

            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            _pipe = new Pipe(options);

            _frameWriter = new Http2FrameWriter(
                new NullPipeWriter(),
                connectionContext: null,
                http2Connection: null,
                new OutputFlowControl(new SingleAwaitableProvider(), initialWindowSize: uint.MaxValue),
                timeoutControl: null,
                minResponseDataRate: null,
                "TestConnectionId",
                _memoryPool,
                new KestrelTrace(NullLogger.Instance));

            _responseHeaders = new HttpResponseHeaders();
            _responseHeaders.HeaderContentType = "application/json";
            _responseHeaders.HeaderContentLength = "1024";
        }

        [Benchmark]
        public void WriteResponseHeaders()
        {
            _frameWriter.WriteResponseHeaders(0, 200, Http2HeadersFrameFlags.END_HEADERS, _responseHeaders);
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _pipe.Writer.Complete();
            _memoryPool?.Dispose();
        }
    }
}
