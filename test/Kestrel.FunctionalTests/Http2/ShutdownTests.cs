// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_2

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.Http2
{
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/corefx/issues/30492")]
    [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Curl requires a custom install to support HTTP/2, see https://askubuntu.com/questions/884899/how-do-i-install-curl-with-http2-support")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public class ShutdownTests : TestApplicationErrorLoggerLoggedTest
    {
        private static X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

        private HttpClient Client { get; set; }
        private List<Http2Frame> ReceivedFrames { get; } = new List<Http2Frame>();

        public ShutdownTests()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // We don't want the default SocketsHttpHandler, it doesn't support HTTP/2 yet.
                Client = new HttpClient(new WinHttpHandler()
                {
                    ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                });
            }
        }

        [ConditionalFact]
        public async Task GracefulShutdownWaitsForRequestsToFinish()
        {
            var requestStarted = new TaskCompletionSource<object>();
            var requestUnblocked = new TaskCompletionSource<object>();
            using (var server = new TestServer(async context =>
            {
                requestStarted.SetResult(null);
                await requestUnblocked.Task.DefaultTimeout();
                await context.Response.WriteAsync("hello world " + context.Request.Protocol);
            },
            new TestServiceContext(LoggerFactory),
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

                // Unblock the request
                requestUnblocked.SetResult(null);

                Assert.Equal("hello world HTTP/2", await requestTask);
                await stopTask.DefaultTimeout();
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Request finished in"));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closing."));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closed. The last processed stream ID was 1."));
        }

        [ConditionalFact]
        public async Task GracefulTurnsAbortiveIfRequestsDoNotFinish()
        {
            var requestStarted = new TaskCompletionSource<object>();
            var requestUnblocked = new TaskCompletionSource<object>();
            // Abortive shutdown leaves one request hanging
            using (var server = new TestServer(TransportSelector.GetWebHostBuilder(new DiagnosticMemoryPoolFactory(allowLateReturn: true).Create), async context =>
            {
                requestStarted.SetResult(null);
                await requestUnblocked.Task.DefaultTimeout();
                await context.Response.WriteAsync("hello world " + context.Request.Protocol);
            }, new TestServiceContext(LoggerFactory),
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

                await server.StopAsync().DefaultTimeout();
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closing."));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("is closed. The last processed stream ID was 1."));
            Assert.Contains(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Some connections failed to close gracefully during server shutdown."));
            Assert.DoesNotContain(TestApplicationErrorLogger.Messages, m => m.Message.Contains("Request finished in"));
        }
    }
}
#elif NET461 // No ALPN support
#else
#error TFMs need updating
#endif