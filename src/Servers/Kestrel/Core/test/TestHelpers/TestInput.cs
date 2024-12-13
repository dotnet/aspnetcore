// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

class TestInput : IDisposable
{
    private readonly MemoryPool<byte> _memoryPool;

    public TestInput(KestrelTrace log = null, ITimeoutControl timeoutControl = null)
    {
        _memoryPool = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(pool: _memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);
        Transport = pair.Transport;
        Application = pair.Application;

        var connectionContext = Mock.Of<ConnectionContext>();
        var metricsContext = TestContextFactory.CreateMetricsContext(connectionContext);

        var connectionFeatures = new FeatureCollection();
        connectionFeatures.Set(Mock.Of<IConnectionLifetimeFeature>());
        connectionFeatures.Set<IConnectionMetricsContextFeature>(new TestConnectionMetricsContextFeature { MetricsContext = metricsContext });

        Http1ConnectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: new TestServiceContext
            {
                Log = log ?? new KestrelTrace(NullLoggerFactory.Instance)
            },
            connectionContext: connectionContext,
            transport: Transport,
            timeoutControl: timeoutControl ?? Mock.Of<ITimeoutControl>(),
            memoryPool: _memoryPool,
            connectionFeatures: connectionFeatures,
            metricsContext: metricsContext);

        Http1Connection = new Http1Connection(Http1ConnectionContext);
        Http1Connection.HttpResponseControl = Mock.Of<IHttpResponseControl>();
        Http1Connection.Reset();
    }

    public IDuplexPipe Transport { get; }

    public IDuplexPipe Application { get; }

    public HttpConnectionContext Http1ConnectionContext { get; }

    public Http1Connection Http1Connection { get; set; }

    public void Add(string text)
    {
        var data = Encoding.ASCII.GetBytes(text);
        async Task Write() => await Application.Output.WriteAsync(data);
        Write().Wait();
    }

    public void Fin()
    {
        Application.Output.Complete();
    }

    public void Cancel()
    {
        Transport.Input.CancelPendingRead();
    }

    public void Dispose()
    {
        Application.Input.Complete();
        Application.Output.Complete();
        Transport.Input.Complete();
        Transport.Output.Complete();
        _memoryPool.Dispose();
    }
}

