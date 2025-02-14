// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class Http1ReadingBenchmark
{
    // Standard completed task
    private static readonly Func<object, Task> _syncTaskFunc = (obj) => Task.CompletedTask;
    // Non-standard completed task
    private static readonly Task _pseudoAsyncTask = Task.FromResult(27);
    private static readonly Func<object, Task> _pseudoAsyncTaskFunc = (obj) => _pseudoAsyncTask;

    private TestHttp1Connection _http1Connection;
    private DuplexPipe.DuplexPipePair _pair;
    private MemoryPool<byte> _memoryPool;

    private readonly byte[] _readData = Encoding.ASCII.GetBytes(new string('a', 100));

    [GlobalSetup]
    public void GlobalSetup()
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        _http1Connection = MakeHttp1Connection();
    }

    [Params(true, false)]
    public bool WithHeaders { get; set; }

    //[Params(true, false)]
    //public bool Chunked { get; set; }

    [Params(Startup.None, Startup.Sync, Startup.Async)]
    public Startup OnStarting { get; set; }

    [IterationSetup]
    public void Setup()
    {
        _http1Connection.Reset();

        _http1Connection.RequestHeaders.ContentLength = _readData.Length;

        if (!WithHeaders)
        {
            _http1Connection.FlushAsync().GetAwaiter().GetResult();
        }

        ResetState();

        _pair.Application.Output.WriteAsync(_readData).GetAwaiter().GetResult();
    }

    private void ResetState()
    {
        if (WithHeaders)
        {
            _http1Connection.ResetState();

            switch (OnStarting)
            {
                case Startup.Sync:
                    _http1Connection.OnStarting(_syncTaskFunc, null);
                    break;
                case Startup.Async:
                    _http1Connection.OnStarting(_pseudoAsyncTaskFunc, null);
                    break;
            }
        }
    }

    [Benchmark]
    public Task ReadAsync()
    {
        ResetState();

        return _http1Connection.RequestBody.ReadAsync(new byte[100], default(CancellationToken)).AsTask();
    }

    private TestHttp1Connection MakeHttp1Connection()
    {
        var options = new PipeOptions(_memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
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
        http1Connection.InitializeBodyControl(new Http1ContentLengthMessageBody(http1Connection, contentLength: 100, keepAlive: true));
        serviceContext.DateHeaderValueManager.OnHeartbeat();

        return http1Connection;
    }

    [IterationCleanup]
    public void Cleanup()
    {
        var reader = _pair.Application.Input;
        if (reader.TryRead(out var readResult))
        {
            reader.AdvanceTo(readResult.Buffer.End);
        }
    }

    public enum Startup
    {
        None,
        Sync,
        Async
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _memoryPool?.Dispose();
    }
}
