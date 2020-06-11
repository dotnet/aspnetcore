// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    internal class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            var logger = new TestApplicationErrorLogger();
            var kestrelTrace = new TestKestrelTrace(logger);
            var loggerFactory = new LoggerFactory(new[] { new KestrelTestLoggerProvider(logger) });

            Initialize(loggerFactory, kestrelTrace);
        }

        public TestServiceContext(ILoggerFactory loggerFactory)
        {
            Initialize(loggerFactory, CreateLoggingTrace(loggerFactory));
        }

        public TestServiceContext(ILoggerFactory loggerFactory, IKestrelTrace kestrelTrace)
        {
            Initialize(loggerFactory, new CompositeKestrelTrace(kestrelTrace, CreateLoggingTrace(loggerFactory)));
        }

        private static KestrelTrace CreateLoggingTrace(ILoggerFactory loggerFactory)
        {
            return new KestrelTrace(loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"));
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

        private void Initialize(ILoggerFactory loggerFactory, IKestrelTrace kestrelTrace)
        {
            LoggerFactory = loggerFactory;
            Log = kestrelTrace;
            Scheduler = PipeScheduler.ThreadPool;
            MockSystemClock = new MockSystemClock();
            SystemClock = MockSystemClock;
            DateHeaderValueManager = new DateHeaderValueManager();
            ConnectionManager = new ConnectionManager(Log, ResourceCounter.Unlimited);
            HttpParser = new HttpParser<Http1ParsingHandler>(Log.IsEnabled(LogLevel.Information));
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false
            };

            DateHeaderValueManager.OnHeartbeat(SystemClock.UtcNow);
        }

        public ILoggerFactory LoggerFactory { get; set; }

        public MockSystemClock MockSystemClock { get; set; }

        public Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = System.Buffers.SlabMemoryPoolFactory.Create;

        public string DateHeaderValue => DateHeaderValueManager.GetDateHeaderValues().String;
    }
}
