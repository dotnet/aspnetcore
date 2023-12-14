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

public class RequestParsingBenchmark
{
    private MemoryPool<byte> _memoryPool;

    public Pipe Pipe { get; set; }

    internal Http1Connection Http1Connection { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            httpParser: new HttpParser<Http1ParsingHandler>(),
            dateHeaderValueManager: new DateHeaderValueManager(TimeProvider.System));

        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: null,
            transport: pair.Transport,
            memoryPool: _memoryPool,
            connectionFeatures: new FeatureCollection(),
            timeoutControl: new TimeoutControl(timeoutHandler: null, TimeProvider.System));

        var http1Connection = new Http1Connection(connectionContext);

        http1Connection.Reset();

        Http1Connection = http1Connection;
        Pipe = new Pipe(new PipeOptions(_memoryPool));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void PlaintextTechEmpower()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.PlaintextTechEmpowerRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void PlaintextAbsoluteUri()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.PlaintextAbsoluteUriRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
    public void PipelinedPlaintextTechEmpower()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.PlaintextTechEmpowerPipelinedRequests);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
    public void PipelinedPlaintextTechEmpowerDrainBuffer()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.PlaintextTechEmpowerPipelinedRequests);
            ParseDataDrainBuffer();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void LiveAspNet()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.LiveaspnetRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
    public void PipelinedLiveAspNet()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.LiveaspnetPipelinedRequests);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount)]
    public void Unicode()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.UnicodeRequest);
            ParseData();
        }
    }

    [Benchmark(OperationsPerInvoke = RequestParsingData.InnerLoopCount * RequestParsingData.Pipelining)]
    public void UnicodePipelined()
    {
        for (var i = 0; i < RequestParsingData.InnerLoopCount; i++)
        {
            InsertData(RequestParsingData.UnicodePipelinedRequests);
            ParseData();
        }
    }

    private void InsertData(byte[] bytes)
    {
        Pipe.Writer.Write(bytes);
        // There should not be any backpressure and task completes immediately
        Pipe.Writer.FlushAsync().GetAwaiter().GetResult();
    }

    private void ParseDataDrainBuffer()
    {
        var awaitable = Pipe.Reader.ReadAsync();
        if (!awaitable.IsCompleted)
        {
            // No more data
            return;
        }

        var readableBuffer = awaitable.GetAwaiter().GetResult().Buffer;
        var reader = new SequenceReader<byte>(readableBuffer);
        do
        {
            Http1Connection.Reset();

            if (!Http1Connection.TakeStartLine(ref reader))
            {
                ErrorUtilities.ThrowInvalidRequestLine();
            }

            if (!Http1Connection.TakeMessageHeaders(ref reader, trailers: false))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
        }
        while (!reader.End);

        Pipe.Reader.AdvanceTo(readableBuffer.End);
    }

    private void ParseData()
    {
        do
        {
            var awaitable = Pipe.Reader.ReadAsync();
            if (!awaitable.IsCompleted)
            {
                // No more data
                return;
            }

            var result = awaitable.GetAwaiter().GetResult();
            var readableBuffer = result.Buffer;
            var reader = new SequenceReader<byte>(readableBuffer);

            Http1Connection.Reset();

            if (!Http1Connection.TakeStartLine(ref reader))
            {
                ErrorUtilities.ThrowInvalidRequestLine();
            }
            Pipe.Reader.AdvanceTo(reader.Position, reader.Position);

            result = Pipe.Reader.ReadAsync().GetAwaiter().GetResult();
            readableBuffer = result.Buffer;
            reader = new SequenceReader<byte>(readableBuffer);

            if (!Http1Connection.TakeMessageHeaders(ref reader, trailers: false))
            {
                ErrorUtilities.ThrowInvalidRequestHeaders();
            }
            Pipe.Reader.AdvanceTo(reader.Position, reader.Position);
        }
        while (true);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _memoryPool.Dispose();
    }
}
