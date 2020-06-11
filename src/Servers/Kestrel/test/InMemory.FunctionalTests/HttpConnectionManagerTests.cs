// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
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

            var mockTrace = new Mock<KestrelTrace>(Logger) { CallBase = true };
            mockTrace
                .Setup(trace => trace.ApplicationNeverCompleted(It.IsAny<string>()))
                .Callback(() =>
                {
                    logWh.Release();
                });

            var testContext = new TestServiceContext(new LoggerFactory(), mockTrace.Object);
            testContext.InitializeHeartbeat();

            await using (var server = new TestServer(context =>
                {
                    appStartedWh.Release();
                    var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
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
#endif

        private class NoDebuggerConditionAttribute : Attribute, ITestCondition
        {
            public bool IsMet => !Debugger.IsAttached;
            public string SkipReason => "A debugger is attached.";
        }
    }
}
