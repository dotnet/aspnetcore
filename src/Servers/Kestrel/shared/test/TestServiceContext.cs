// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics.Metrics;
using System.IO.Pipelines;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Testing;

internal class TestServiceContext : ServiceContext
{
    public TestServiceContext() : this(disableHttp1LineFeedTerminators: true)
    {
    }

    public TestServiceContext(ILoggerFactory loggerFactory = null, KestrelTrace kestrelTrace = null, bool disableHttp1LineFeedTerminators = true, KestrelMetrics metrics = null)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        kestrelTrace ??= CreateLoggingTrace(loggerFactory);
        metrics ??= new KestrelMetrics(new TestMeterFactory());

        Initialize(loggerFactory, kestrelTrace, disableHttp1LineFeedTerminators, metrics);
    }

    private static KestrelTrace CreateLoggingTrace(ILoggerFactory loggerFactory)
    {
        return new KestrelTrace(loggerFactory);
    }

    public void InitializeHeartbeat()
    {
        DateHeaderValueManager = new DateHeaderValueManager(TimeProvider.System);
        Heartbeat = new Heartbeat(
            new IHeartbeatHandler[] { DateHeaderValueManager, ConnectionManager },
            TimeProvider.System,
            DebuggerWrapper.Singleton,
            Log,
            Heartbeat.Interval);

        MockTimeProvider = null;
        TimeProvider = TimeProvider.System;
    }

    private void Initialize(ILoggerFactory loggerFactory, KestrelTrace kestrelTrace, bool disableHttp1LineFeedTerminators, KestrelMetrics metrics)
    {
        LoggerFactory = loggerFactory;
        Log = kestrelTrace;
        Scheduler = PipeScheduler.ThreadPool;
        MockTimeProvider = new MockTimeProvider();
        TimeProvider = MockTimeProvider;
        DateHeaderValueManager = new DateHeaderValueManager(MockTimeProvider);
        ConnectionManager = new ConnectionManager(Log, ResourceCounter.Unlimited);
        HttpParser = new HttpParser<Http1ParsingHandler>(Log.IsEnabled(LogLevel.Information), disableHttp1LineFeedTerminators);
        ServerOptions = new KestrelServerOptions
        {
            AddServerHeader = false
        };

        DateHeaderValueManager.OnHeartbeat();
        Metrics = metrics;
    }

    public ILoggerFactory LoggerFactory { get; set; }

    public MockTimeProvider MockTimeProvider { get; set; }

    public Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.PinnedBlockMemoryPoolFactory.Create;

    public string DateHeaderValue => DateHeaderValueManager.GetDateHeaderValues().String;
}
