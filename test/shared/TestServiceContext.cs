// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            var logger = new TestApplicationErrorLogger();

            Log = new TestKestrelTrace(logger);
            ThreadPool = new LoggingThreadPool(Log);
            SystemClock = new MockSystemClock();
            DateHeaderValueManager = new DateHeaderValueManager(SystemClock);
            ConnectionManager = new FrameConnectionManager(Log);
            DateHeaderValue = DateHeaderValueManager.GetDateHeaderValues().String;
            HttpParserFactory = frameAdapter => new HttpParser<FrameAdapter>(frameAdapter.Frame.ServiceContext.Log);
            ServerOptions = new KestrelServerOptions
            {
                AddServerHeader = false
            };
        }

        public string DateHeaderValue { get; }
    }
}
