// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class FrameConnectionManagerTests
    {
        private const int _applicationNeverCompletedId = 23;

        [ConditionalFact]
        [NoDebuggerCondition]
        public async Task CriticalErrorLoggedIfApplicationDoesntComplete()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            // WARNING: This test will fail under a debugger because Task.s_currentActiveTasks    //
            //          roots FrameConnection.                                                    //
            ////////////////////////////////////////////////////////////////////////////////////////

            var logWh = new SemaphoreSlim(0);
            var appStartedWh = new SemaphoreSlim(0);

            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                .Setup(logger => logger.Log(LogLevel.Critical, _applicationNeverCompletedId, It.IsAny<object>(), null,
                    It.IsAny<Func<object, Exception, string>>()))
                .Callback(() =>
                {
                    logWh.Release();
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"))
                .Returns(mockLogger.Object);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsNotIn("Microsoft.AspNetCore.Server.Kestrel")))
                .Returns(Mock.Of<ILogger>());

            var builder = new WebHostBuilder()
                .UseLoggerFactory(mockLoggerFactory.Object)
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        appStartedWh.Release();
                        var tcs = new TaskCompletionSource<object>();
                        return tcs.Task;
                    });
                });

            using (var host = builder.Build())
            {
                host.Start();

                using (var connection = new TestConnection(host.GetPort()))
                {
                    await connection.Send("GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    Assert.True(await appStartedWh.WaitAsync(TimeSpan.FromSeconds(10)));

                    // Close connection without waiting for a response
                }

                var logWaitAttempts = 0;

                for (; !await logWh.WaitAsync(TimeSpan.FromSeconds(1)) && logWaitAttempts < 10; logWaitAttempts++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                Assert.True(logWaitAttempts < 10);
            }
        }

        private class NoDebuggerConditionAttribute : Attribute, ITestCondition
        {
            public bool IsMet => !Debugger.IsAttached;
            public string SkipReason => "A debugger is attached.";
        }
    }
}
