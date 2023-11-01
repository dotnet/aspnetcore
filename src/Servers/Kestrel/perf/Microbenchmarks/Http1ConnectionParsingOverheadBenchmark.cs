// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class Http1ConnectionParsingOverheadBenchmark
{
    private const int InnerLoopCount = 512;

    public ReadOnlySequence<byte> _buffer;
    private Http1Connection _http1Connection;

    [IterationSetup]
    public void Setup()
    {
        var memoryPool = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            httpParser: NullParser<Http1ParsingHandler>.Instance);

        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: null,
            transport: pair.Transport,
            timeoutControl: new TimeoutControl(timeoutHandler: null, TimeProvider.System),
            memoryPool: memoryPool,
            connectionFeatures: new FeatureCollection());

        var http1Connection = new Http1Connection(connectionContext);

        http1Connection.Reset();

        _http1Connection = http1Connection;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = InnerLoopCount)]
    public void Http1ConnectionOverheadTotal()
    {
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ParseRequest();
        }
    }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public void Http1ConnectionOverheadRequestLine()
    {
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ParseRequestLine();
        }
    }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public void Http1ConnectionOverheadRequestHeaders()
    {
        for (var i = 0; i < InnerLoopCount; i++)
        {
            ParseRequestHeaders();
        }
    }

    private void ParseRequest()
    {
        _http1Connection.Reset();

        var reader = new SequenceReader<byte>(_buffer);
        if (!_http1Connection.TakeStartLine(ref reader))
        {
            ErrorUtilities.ThrowInvalidRequestLine();
        }

        if (!_http1Connection.TakeMessageHeaders(ref reader, trailers: false))
        {
            ErrorUtilities.ThrowInvalidRequestHeaders();
        }
    }

    private void ParseRequestLine()
    {
        _http1Connection.Reset();

        var reader = new SequenceReader<byte>(_buffer);
        if (!_http1Connection.TakeStartLine(ref reader))
        {
            ErrorUtilities.ThrowInvalidRequestLine();
        }
    }

    private void ParseRequestHeaders()
    {
        _http1Connection.Reset();

        var reader = new SequenceReader<byte>(_buffer);
        if (!_http1Connection.TakeMessageHeaders(ref reader, trailers: false))
        {
            ErrorUtilities.ThrowInvalidRequestHeaders();
        }
    }
}
