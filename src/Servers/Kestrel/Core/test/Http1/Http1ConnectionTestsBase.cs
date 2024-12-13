// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http1ConnectionTestsBase : LoggedTest, IDisposable
{
    internal IDuplexPipe _transport;
    internal IDuplexPipe _application;
    internal TestHttp1Connection _http1Connection;
    internal ServiceContext _serviceContext;
    internal HttpConnectionContext _http1ConnectionContext;
    internal MemoryPool<byte> _pipelineFactory;
    internal SequencePosition _consumed;
    internal SequencePosition _examined;
    internal Mock<ITimeoutControl> _timeoutControl;

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

        _pipelineFactory = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(_pipelineFactory, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);

        _transport = pair.Transport;
        _application = pair.Application;

        var connectionContext = Mock.Of<ConnectionContext>();
        var metricsContext = TestContextFactory.CreateMetricsContext(connectionContext);

        var connectionFeatures = new FeatureCollection();
        connectionFeatures.Set(Mock.Of<IConnectionLifetimeFeature>());
        connectionFeatures.Set<IConnectionMetricsContextFeature>(new TestConnectionMetricsContextFeature { MetricsContext = metricsContext });

        _serviceContext = new TestServiceContext(LoggerFactory)
        {
            Scheduler = PipeScheduler.Inline
        };

        _timeoutControl = new Mock<ITimeoutControl>();
        _http1ConnectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: _serviceContext,
            connectionContext: Mock.Of<ConnectionContext>(),
            transport: pair.Transport,
            timeoutControl: _timeoutControl.Object,
            memoryPool: _pipelineFactory,
            connectionFeatures: connectionFeatures,
            metricsContext: metricsContext);

        _http1Connection = new TestHttp1Connection(_http1ConnectionContext);
    }

    void IDisposable.Dispose()
    {
        base.Dispose();

        _transport.Input.Complete();
        _transport.Output.Complete();

        _application.Input.Complete();
        _application.Output.Complete();

        _pipelineFactory.Dispose();
    }
}
