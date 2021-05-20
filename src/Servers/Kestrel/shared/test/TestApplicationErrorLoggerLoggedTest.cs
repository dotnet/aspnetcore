// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.Testing.TestApplicationErrorLogger;

namespace Microsoft.AspNetCore.Testing
{
    public class TestApplicationErrorLoggerLoggedTest : LoggedTest
    {
        private TestApplicationErrorLogger TestApplicationErrorLogger { get; set; }

        public ConcurrentQueue<LogMessage> LogMessages => TestApplicationErrorLogger.Messages;

        public bool ThrowOnCriticalErrors
        {
            get => TestApplicationErrorLogger.ThrowOnCriticalErrors;
            set => TestApplicationErrorLogger.ThrowOnCriticalErrors = value;
        }

        public bool ThrowOnUngracefulShutdown
        {
            get => TestApplicationErrorLogger.ThrowOnUngracefulShutdown;
            set => TestApplicationErrorLogger.ThrowOnUngracefulShutdown = value;
        }

        public List<Type> IgnoredCriticalLogExceptions => TestApplicationErrorLogger.IgnoredExceptions;

        public Task<LogMessage> WaitForLogMessage(Func<LogMessage, bool> messageFilter)
            => TestApplicationErrorLogger.WaitForMessage(messageFilter);

        public override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

            TestApplicationErrorLogger = new TestApplicationErrorLogger();
            LoggerFactory.AddProvider(new KestrelTestLoggerProvider(TestApplicationErrorLogger));
        }
    }
}
