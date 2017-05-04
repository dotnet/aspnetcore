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
        {
            ErrorLogger = new TestApplicationErrorLogger();
            Log = new TestKestrelTrace(ErrorLogger);
            ThreadPool = new LoggingThreadPool(Log);
            SystemClock = new MockSystemClock();
            DateHeaderValueManager = new DateHeaderValueManager(SystemClock);
            ConnectionManager = new FrameConnectionManager(Log);
            DateHeaderValue = DateHeaderValueManager.GetDateHeaderValues().String;
            HttpParserFactory = frameAdapter => new HttpParser<FrameAdapter>(frameAdapter.Frame.ServiceContext.Log.IsEnabled(LogLevel.Information));
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false
            };
        }

        public TestApplicationErrorLogger ErrorLogger { get; }

        public string DateHeaderValue { get; }
    }
}
