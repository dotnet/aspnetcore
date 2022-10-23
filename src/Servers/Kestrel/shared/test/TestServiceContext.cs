// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Testing;

internal class TestServiceContext : ServiceContext
{
    public TestServiceContext()
    {
        Initialize(NullLoggerFactory.Instance, CreateLoggingTrace(NullLoggerFactory.Instance), false);
    }

    public TestServiceContext(ILoggerFactory loggerFactory, bool disableHttp1LineFeedTerminators = true)
    {
        Initialize(loggerFactory, CreateLoggingTrace(loggerFactory), disableHttp1LineFeedTerminators);
    }

    public TestServiceContext(ILoggerFactory loggerFactory, KestrelTrace kestrelTrace, bool disableHttp1LineFeedTerminators = true)
    {
        Initialize(loggerFactory, kestrelTrace, disableHttp1LineFeedTerminators);
    }

    private static KestrelTrace CreateLoggingTrace(ILoggerFactory loggerFactory)
    {
        return new KestrelTrace(loggerFactory);
    }

    public void InitializeHeartbeat()
    {
        var heartbeatManager = new HeartbeatManager(ConnectionManager);
        DateHeaderValueManager = new DateHeaderValueManager();
        Heartbeat = new Heartbeat(
            new IHeartbeatHandler[] { DateHeaderValueManager, heartbeatManager },
            new SystemClock(),
            DebuggerWrapper.Singleton,
            Log);

        MockSystemClock = null;
        SystemClock = heartbeatManager;
    }

    private void Initialize(ILoggerFactory loggerFactory, KestrelTrace kestrelTrace, bool disableHttp1LineFeedTerminators)
    {
        LoggerFactory = loggerFactory;
        Log = kestrelTrace;
        Scheduler = PipeScheduler.ThreadPool;
        MockSystemClock = new MockSystemClock();
        SystemClock = MockSystemClock;
        DateHeaderValueManager = new DateHeaderValueManager();
        ConnectionManager = new ConnectionManager(Log, ResourceCounter.Unlimited);
        HttpParser = new HttpParser<Http1ParsingHandler>(Log.IsEnabled(LogLevel.Information), disableHttp1LineFeedTerminators);
        ServerOptions = new KestrelServerOptions
        {
            AddServerHeader = false
        };

        DateHeaderValueManager.OnHeartbeat(SystemClock.UtcNow);
    }

    public ILoggerFactory LoggerFactory { get; set; }

    public MockSystemClock MockSystemClock { get; set; }

    public Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.PinnedBlockMemoryPoolFactory.Create;

    public string DateHeaderValue => DateHeaderValueManager.GetDateHeaderValues().String;
}
