// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;
using static Microsoft.AspNetCore.InternalTesting.TestApplicationErrorLogger;

namespace Microsoft.AspNetCore.InternalTesting;

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

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);

        TestApplicationErrorLogger = new TestApplicationErrorLogger();
        LoggerFactory.AddProvider(new KestrelTestLoggerProvider(TestApplicationErrorLogger));
    }
}
