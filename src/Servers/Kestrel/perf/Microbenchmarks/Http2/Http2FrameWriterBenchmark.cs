// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

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
            dateHeaderValueManager: new DateHeaderValueManager());

        _frameWriter = new Http2FrameWriter(
            new NullPipeWriter(),
            connectionContext: null,
            http2Connection: null,
            maxStreamsPerConnection: 1,
            timeoutControl: null,
            minResponseDataRate: null,
            "TestConnectionId",
            _memoryPool,
            serviceContext);

        _responseHeaders = new HttpResponseHeaders();
        var headers = (IHeaderDictionary)_responseHeaders;
        headers.ContentType = "application/json";
        headers.ContentLength = 1024;
    }

    [Benchmark]
    public void WriteResponseHeaders()
    {
        _frameWriter.WriteResponseHeaders(streamId: 0, 200, Http2HeadersFrameFlags.END_STREAM, _responseHeaders);
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _pipe.Writer.Complete();
        _memoryPool?.Dispose();
    }
}
