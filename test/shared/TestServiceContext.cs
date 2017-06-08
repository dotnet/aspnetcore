// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            : this(new LoggerFactory(new[] { new KestrelTestLoggerProvider() }))
        {
        }

        public TestServiceContext(ILoggerFactory loggerFactory)
            : this(loggerFactory, new KestrelTrace(loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel")))
        {
        }

        public TestServiceContext(ILoggerFactory loggerFactory, IKestrelTrace kestrelTrace)
        {
            LoggerFactory = loggerFactory;
            Log = kestrelTrace;
            ThreadPool = new LoggingThreadPool(Log);
            SystemClock = new MockSystemClock();
            DateHeaderValueManager = new DateHeaderValueManager(SystemClock);
            ConnectionManager = new FrameConnectionManager(Log, ResourceCounter.Unlimited, ResourceCounter.Unlimited);
            HttpParserFactory = frameAdapter => new HttpParser<FrameAdapter>(frameAdapter.Frame.ServiceContext.Log.IsEnabled(LogLevel.Information));
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false
            };
        }

        public ILoggerFactory LoggerFactory { get; }

        public string DateHeaderValue => DateHeaderValueManager.GetDateHeaderValues().String;
    }
}
