// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class Http1LargeWritingBenchmark
{
    private TestHttp1Connection _http1Connection;
    private DuplexPipe.DuplexPipePair _pair;
    private MemoryPool<byte> _memoryPool;
    private Task _consumeResponseBodyTask;

    // Keep this divisable by 10 so it can be evenly segmented.
    private readonly byte[] _writeData = new byte[10 * 1024 * 1024];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        _http1Connection = MakeHttp1Connection();
        _consumeResponseBodyTask = ConsumeResponseBody();
    }

    [IterationSetup]
    public void Setup()
    {
        _http1Connection.Reset();
        _http1Connection.RequestHeaders.ContentLength = _writeData.Length;
        _http1Connection.FlushAsync().GetAwaiter().GetResult();
    }

    [Benchmark]
    public Task WriteAsync()
    {
        return _http1Connection.ResponseBody.WriteAsync(_writeData, 0, _writeData.Length, default);
    }

    [Benchmark]
    public Task WriteSegmentsUnawaitedAsync()
    {
        // Write a 10th the of the data at a time
        var segmentSize = _writeData.Length / 10;

        for (int i = 0; i < 9; i++)
        {
            // Ignore the first nine tasks.
            _ = _http1Connection.ResponseBody.WriteAsync(_writeData, i * segmentSize, segmentSize, default);
        }

        return _http1Connection.ResponseBody.WriteAsync(_writeData, 9 * segmentSize, segmentSize, default);
    }

    private TestHttp1Connection MakeHttp1Connection()
    {
        var options = new PipeOptions(_memoryPool, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);
        _pair = pair;

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            httpParser: new HttpParser<Http1ParsingHandler>(),
            dateHeaderValueManager: new DateHeaderValueManager(TimeProvider.System));

        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: null,
            transport: pair.Transport,
            timeoutControl: new TimeoutControl(timeoutHandler: null, TimeProvider.System),
            memoryPool: _memoryPool,
            connectionFeatures: new FeatureCollection());

        var http1Connection = new TestHttp1Connection(connectionContext);

        http1Connection.Reset();
        http1Connection.InitializeBodyControl(MessageBody.ZeroContentLengthKeepAlive);
        serviceContext.DateHeaderValueManager.OnHeartbeat();

        return http1Connection;
    }

    private async Task ConsumeResponseBody()
    {
        var reader = _pair.Application.Input;
        var readResult = await reader.ReadAsync();

        while (!readResult.IsCompleted)
        {
            reader.AdvanceTo(readResult.Buffer.End);
            readResult = await reader.ReadAsync();
        }

        reader.Complete();
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _pair.Transport.Output.Complete();
        _consumeResponseBodyTask.GetAwaiter().GetResult();
        _memoryPool?.Dispose();
    }
}
