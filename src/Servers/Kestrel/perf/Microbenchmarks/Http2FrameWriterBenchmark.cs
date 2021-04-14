// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks
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
            _memoryPool = PinnedBlockMemoryPoolFactory.Create();

            var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            _pipe = new Pipe(options);

            var serviceContext = TestContextFactory.CreateServiceContext(
                serverOptions: new KestrelServerOptions(),
                httpParser: new HttpParser<Http1ParsingHandler>(),
                dateHeaderValueManager: new DateHeaderValueManager(),
                log: new MockTrace());

            _frameWriter = new Http2FrameWriter(
                new NullPipeWriter(),
                connectionContext: null,
                http2Connection: null,
                new OutputFlowControl(new SingleAwaitableProvider(), initialWindowSize: int.MaxValue),
                timeoutControl: null,
                minResponseDataRate: null,
                "TestConnectionId",
                _memoryPool,
                serviceContext);

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
