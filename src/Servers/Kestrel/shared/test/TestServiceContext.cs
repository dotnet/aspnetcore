// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            var logger = new TestApplicationErrorLogger();
            var kestrelTrace = new TestKestrelTrace(logger);
            var loggerFactory = new LoggerFactory(new[] { new KestrelTestLoggerProvider(logger) });

            Initialize(loggerFactory, kestrelTrace);
        }

        public TestServiceContext(ILoggerFactory loggerFactory)
            : this(loggerFactory, new KestrelTrace(loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel")))
        {
        }

        public TestServiceContext(ILoggerFactory loggerFactory, IKestrelTrace kestrelTrace)
        {
            Initialize(loggerFactory, kestrelTrace);
        }

        private void Initialize(ILoggerFactory loggerFactory, IKestrelTrace kestrelTrace)
        {
            LoggerFactory = loggerFactory;
            Log = kestrelTrace;
            Scheduler = PipeScheduler.ThreadPool;
            SystemClock = new MockSystemClock();
            DateHeaderValueManager = new DateHeaderValueManager(SystemClock);
            ConnectionManager = new HttpConnectionManager(Log, ResourceCounter.Unlimited);
            HttpParser = new HttpParser<Http1ParsingHandler>(Log.IsEnabled(LogLevel.Information));
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false
            };
        }

        public ILoggerFactory LoggerFactory { get; set; }

        public string DateHeaderValue => DateHeaderValueManager.GetDateHeaderValues().String;
    }
}
