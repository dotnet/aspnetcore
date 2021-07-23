// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Testing
{
    internal class TestKestrelTrace : KestrelTrace
    {
        public TestKestrelTrace() : this(new TestApplicationErrorLogger())
        {
        }

        public TestKestrelTrace(TestApplicationErrorLogger testLogger) : this(new LoggerFactory(new[] { new KestrelTestLoggerProvider(testLogger) }))
        {
            Logger = testLogger;
        }

        private TestKestrelTrace(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            LoggerFactory = loggerFactory;
        }

        public TestApplicationErrorLogger Logger { get; }
        public ILoggerFactory LoggerFactory { get; }
    }
}
