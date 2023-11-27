// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class HttpConnectionManagerTests : LoggedTest
{
    // This test causes MemoryPoolBlocks to be finalized which in turn causes an assert failure in debug builds.
#if !DEBUG
    [ConditionalFact]
    [NoDebuggerCondition]
    public async Task CriticalErrorLoggedIfApplicationDoesntComplete()
    {
        ////////////////////////////////////////////////////////////////////////////////////////
        // WARNING: This test will fail under a debugger because Task.s_currentActiveTasks    //
        //          roots HttpConnection.                                                     //
        ////////////////////////////////////////////////////////////////////////////////////////

        var logWh = new SemaphoreSlim(0);
        var appStartedWh = new SemaphoreSlim(0);

        var factory = new LoggerFactory();

        // Use a custom logger for callback instead of TestSink because TestSink keeps references
        // to types when logging, prevents garbage collection, and makes the test fail.
        factory.AddProvider(new CallbackLoggerProvider(eventId =>
        {
            if (eventId.Name == "ApplicationNeverCompleted")
            {
                Logger.LogInformation("Releasing ApplicationNeverCompleted log wait handle.");
                logWh.Release();
            }
        }));

        var testContext = new TestServiceContext(factory);
        testContext.InitializeHeartbeat();

        await using (var server = new TestServer(context =>
        {
            appStartedWh.Release();
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return tcs.Task;
        },
        testContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.SendEmptyGet();

                Assert.True(await appStartedWh.WaitAsync(TestConstants.DefaultTimeout));

                // Close connection without waiting for a response
            }

            var logWaitAttempts = 0;

            for (; !await logWh.WaitAsync(TimeSpan.FromSeconds(1)) && logWaitAttempts < 30; logWaitAttempts++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.True(logWaitAttempts < 10);
        }
    }

    private class CallbackLoggerProvider : ILoggerProvider
    {
        private readonly Action<EventId> _logAction;

        public CallbackLoggerProvider(Action<EventId> logAction)
        {
            _logAction = logAction;
        }

        public ILogger CreateLogger(string categoryName) => new CallbackLogger(_logAction);

        public void Dispose()
        {
        }

        private class CallbackLogger : ILogger
        {
            private readonly Action<EventId> _logAction;

            public CallbackLogger(Action<EventId> logAction)
            {
                _logAction = logAction;
            }

            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _logAction(eventId);
            }
        }
    }
#endif

    private class NoDebuggerConditionAttribute : Attribute, ITestCondition
    {
        public bool IsMet => !Debugger.IsAttached;
        public string SkipReason => "A debugger is attached.";
    }
}
