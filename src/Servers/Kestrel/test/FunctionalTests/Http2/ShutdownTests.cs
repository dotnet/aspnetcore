// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.Http2
{
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/10428", Queues = "Debian.8.Amd64;Debian.8.Amd64.Open")] // Debian 8 uses OpenSSL 1.0.1 which does not support HTTP/2
    public class ShutdownTests : TestApplicationErrorLoggerLoggedTest
    {
        private static X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

        private HttpClient Client { get; set; }
        private List<Http2Frame> ReceivedFrames { get; } = new List<Http2Frame>();

        public ShutdownTests()
        {
            Client = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
            {
                DefaultRequestVersion = new Version(2, 0),
            };
        }

        [CollectDump]
        [ConditionalFact]
        [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/9985", Queues = "Fedora.28.Amd64;Fedora.28.Amd64.Open")]
        public async Task GracefulShutdownWaitsForRequestsToFinish()
        {
            var requestStarted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestUnblocked = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestStopping = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockKestrelTrace = new Mock<KestrelTrace>(TestApplicationErrorLogger)
            {
                CallBase = true
            };
            mockKestrelTrace
                .Setup(m => m.Http2ConnectionClosing(It.IsAny<string>()))
                .Callback(() => requestStopping.SetResult(null));

            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object);

            testContext.InitializeHeartbeat();

            using (var server = new TestServer(async context =>
            {
                requestStarted.SetResult(null);
                await requestUnblocked.Task.DefaultTimeout();
                await context.Response.WriteAsync("hello world " + context.Request.Protocol);
            },
            testContext,
            kestrelOptions =>
            {
                kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                    listenOptions.UseHttps(_x509Certificate2);
                });
            }))
            {
                var requestTask = Client.GetStringAsync($"https://localhost:{server.Port}/");
                Assert.False(requestTask.IsCompleted);

                await requestStarted.Task.DefaultTimeout();

                var stopTask = server.StopAsync();

                await requestStopping.Task.DefaultTimeout();

                // Unblock the request
                requestUnblocked.SetResult(null);

                Assert.Equal("hello world HTTP/2", await requestTask);
                await stopTask.DefaultTimeout();
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Request finished "));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closing."));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closed. The last processed stream ID was 1."));
        }

        [ConditionalFact]
        public async Task GracefulTurnsAbortiveIfRequestsDoNotFinish()
        {
            var requestStarted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestUnblocked = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var memoryPoolFactory = new DiagnosticMemoryPoolFactory(allowLateReturn: true);

            var testContext = new TestServiceContext(LoggerFactory)
            {
                MemoryPoolFactory = memoryPoolFactory.Create
            };

            TestApplicationErrorLogger.ThrowOnUngracefulShutdown = false;

            // Abortive shutdown leaves one request hanging
            using (var server = new TestServer(async context =>
            {
                requestStarted.SetResult(null);
                await requestUnblocked.Task.DefaultTimeout();
                await context.Response.WriteAsync("hello world " + context.Request.Protocol);
            },
            testContext,
            kestrelOptions =>
            {
                kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                    listenOptions.UseHttps(_x509Certificate2);
                });
            },
            _ => { }))
            {
                var requestTask = Client.GetStringAsync($"https://localhost:{server.Port}/");
                Assert.False(requestTask.IsCompleted);
                await requestStarted.Task.DefaultTimeout();

                // Wait for the graceful shutdown log before canceling the token passed to StopAsync and triggering an ungraceful shutdown.
                // Otherwise, graceful shutdown might be skipped causing there to be no corresponding log. https://github.com/dotnet/aspnetcore/issues/6556
                var closingMessageTask = TestApplicationErrorLogger.WaitForMessage(m => m.Message.Contains("is closing.")).DefaultTimeout();

                var cts = new CancellationTokenSource();
                var stopServerTask = server.StopAsync(cts.Token).DefaultTimeout();

                await closingMessageTask;
                cts.Cancel();
                await stopServerTask;
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closing."));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closed. The last processed stream ID was 1."));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Some connections failed to close gracefully during server shutdown."));
            Assert.DoesNotContain(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Request finished in"));

            requestUnblocked.SetResult(null);

            await memoryPoolFactory.WhenAllBlocksReturned(TestConstants.DefaultTimeout);
        }
    }
}
