// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestTests
    {
        private const int _semaphoreWaitTimeout = 2500;

        [Theory]
        [InlineData(10 * 1024 * 1024, true)]
        // In the following dataset, send at least 2GB.
        // Never change to a lower value, otherwise regression testing for
        // https://github.com/aspnet/KestrelHttpServer/issues/520#issuecomment-188591242
        // will be lost.
        [InlineData((long)int.MaxValue + 1, false)]
        public void LargeUpload(long contentLength, bool checkBytes)
        {
            const int bufferLength = 1024 * 1024;
            Assert.True(contentLength % bufferLength == 0, $"{nameof(contentLength)} sent must be evenly divisible by {bufferLength}.");
            Assert.True(bufferLength % 256 == 0, $"{nameof(bufferLength)} must be evenly divisible by 256");

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        // Read the full request body
                        long total = 0;
                        var receivedBytes = new byte[bufferLength];
                        var received = 0;
                        while ((received = await context.Request.Body.ReadAsync(receivedBytes, 0, receivedBytes.Length)) > 0)
                        {
                            if (checkBytes)
                            {
                                for (var i = 0; i < received; i++)
                                {
                                    Assert.Equal((byte)((total + i) % 256), receivedBytes[i]);
                                }
                            }

                            total += received;
                        }

                        await context.Response.WriteAsync(total.ToString(CultureInfo.InvariantCulture));
                    });
                });

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes($"POST / HTTP/1.0\r\nContent-Length: {contentLength}\r\n\r\n"));

                    var contentBytes = new byte[bufferLength];

                    if (checkBytes)
                    {
                        for (var i = 0; i < contentBytes.Length; i++)
                        {
                            contentBytes[i] = (byte)i;
                        }
                    }

                    for (var i = 0; i < contentLength / contentBytes.Length; i++)
                    {
                        socket.Send(contentBytes);
                    }

                    var response = new StringBuilder();
                    var responseBytes = new byte[4096];
                    var received = 0;
                    while ((received = socket.Receive(responseBytes)) > 0)
                    {
                        response.Append(Encoding.ASCII.GetString(responseBytes, 0, received));
                    }

                    Assert.Contains(contentLength.ToString(CultureInfo.InvariantCulture), response.ToString());
                }
            }
        }

        [Fact]
        public Task RemoteIPv4Address()
        {
            return TestRemoteIPAddress("127.0.0.1", "127.0.0.1", "127.0.0.1");
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public Task RemoteIPv6Address()
        {
            return TestRemoteIPAddress("[::1]", "[::1]", "::1");
        }

        [Fact]
        public async Task DoesNotHangOnConnectionCloseRequest()
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("hello, world");
                    });
                });

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                host.Start();

                client.DefaultRequestHeaders.Connection.Clear();
                client.DefaultRequestHeaders.Connection.Add("close");

                var response = await client.GetAsync($"http://localhost:{host.GetPort()}/");
                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task StreamsAreNotPersistedAcrossRequests()
        {
            var requestBodyPersisted = false;
            var responseBodyPersisted = false;

            var builder = new WebHostBuilder()
               .UseKestrel()
               .UseUrls($"http://127.0.0.1:0")
               .Configure(app =>
               {
                   app.Run(async context =>
                   {
                       if (context.Request.Body is MemoryStream)
                       {
                           requestBodyPersisted = true;
                       }

                       if (context.Response.Body is MemoryStream)
                       {
                           responseBodyPersisted = true;
                       }

                       context.Request.Body = new MemoryStream();
                       context.Response.Body = new MemoryStream();

                       await context.Response.WriteAsync("hello, world");
                   });
               });

            using (var host = builder.Build())
            {
                host.Start();

                using (var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{host.GetPort()}") })
                {
                    await client.GetAsync("/");
                    await client.GetAsync("/");

                    Assert.False(requestBodyPersisted);
                    Assert.False(responseBodyPersisted);
                }
            }
        }

        [Fact]
        public void CanUpgradeRequestWithConnectionKeepAliveUpgradeHeader()
        {
            var dataRead = false;
            var builder = new WebHostBuilder()
               .UseKestrel()
               .UseUrls($"http://127.0.0.1:0")
               .Configure(app =>
               {
                   app.Run(async context =>
                   {
                       var stream = await context.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                       var data = new byte[3];
                       var bytesRead = 0;

                       while (bytesRead < 3)
                       {
                           bytesRead += await stream.ReadAsync(data, bytesRead, data.Length - bytesRead);
                       }

                       dataRead = Encoding.ASCII.GetString(data, 0, 3) == "abc";
                   });
               });

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nConnection: keep-alive, upgrade\r\n\r\n"));
                    socket.Send(Encoding.ASCII.GetBytes("abc"));

                    while (socket.Receive(new byte[1024]) > 0) ;
                }
            }

            Assert.True(dataRead);
        }

        [Fact]
        public async Task ConnectionResetPriorToRequestIsLoggedAsDebug()
        {
            var connectionStarted = new SemaphoreSlim(0);
            var connectionResetLogged = new SemaphoreSlim(0);
            var testSink = new ConnectionErrorTestSink(
                () => connectionStarted.Release(), () => connectionResetLogged.Release(), () => { });
            var builder = new WebHostBuilder()
                .UseLoggerFactory(new TestLoggerFactory(testSink, true))
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(context =>
                {
                    return Task.FromResult(0);
                }));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    // Wait until connection is established
                    Assert.True(await connectionStarted.WaitAsync(_semaphoreWaitTimeout));

                    // Force a reset
                    socket.LingerState = new LingerOption(true, 0);
                }

                // Wait until connection error is logged
                Assert.True(await connectionResetLogged.WaitAsync(_semaphoreWaitTimeout));

                // Check the no log level higher than Debug was used for a reset before a request
                Assert.Equal(LogLevel.Debug, testSink.MaxLogLevel);

                // Check for expected message
                Assert.NotNull(testSink.ConnectionResetMessage);
                Assert.Contains("reset", testSink.ConnectionResetMessage);
            }
        }

        [Fact]
        public async Task ConnectionResetBetweenRequestsIsLoggedAsDebug()
        {
            var connectionStarted = new SemaphoreSlim(0);
            var connectionResetLogged = new SemaphoreSlim(0);
            var testSink = new ConnectionErrorTestSink(
                () => { }, () => connectionResetLogged.Release(), () => { });
            var builder = new WebHostBuilder()
                .UseLoggerFactory(new TestLoggerFactory(testSink, true))
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(context =>
                {
                    connectionStarted.Release();
                    return Task.FromResult(0);
                }));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n"));

                    // Wait until connection is established
                    Assert.True(await connectionStarted.WaitAsync(_semaphoreWaitTimeout));

                    // Force a reset
                    socket.LingerState = new LingerOption(true, 0);
                }

                // Wait until connection error is logged
                Assert.True(await connectionResetLogged.WaitAsync(_semaphoreWaitTimeout));

                // Check the no log level higher than Debug was used for a reset between a requests
                Assert.Equal(LogLevel.Debug, testSink.MaxLogLevel);

                // Check for expected message
                Assert.NotNull(testSink.ConnectionResetMessage);
                Assert.Contains("reset", testSink.ConnectionResetMessage);
            }
        }

        [Fact]
        public async Task ConnectionResetMidRequestIsLoggedAsInformation()
        {
            var connectionStarted = new SemaphoreSlim(0);
            var connectionResetLogged = new SemaphoreSlim(0);
            var requestProcessingErrorLogged = new SemaphoreSlim(0);
            var testSink = new ConnectionErrorTestSink(
                () => connectionStarted.Release(), () => connectionResetLogged.Release(), () => requestProcessingErrorLogged.Release());
            var builder = new WebHostBuilder()
                .UseLoggerFactory(new TestLoggerFactory(testSink, true))
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(context =>
                {
                    return Task.FromResult(0);
                }));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET"));

                    // Wait until connection is established
                    Assert.True(await connectionStarted.WaitAsync(_semaphoreWaitTimeout));

                    // Ensure "GET" has been processed
                    // Sadly, there is no event/log for starting request line processing
                    await Task.Delay(1000);

                    // Force a reset, give some time for the "GET" to be processed
                    socket.LingerState = new LingerOption(true, 0);
                }

                // Wait until connection error and request processingError is logged
                var waitAsyncResults = await Task.WhenAll(connectionResetLogged.WaitAsync(_semaphoreWaitTimeout), requestProcessingErrorLogged.WaitAsync(_semaphoreWaitTimeout));

                Assert.All(waitAsyncResults, Assert.True);

                // Check the no log level lower than Information was used for a reset mid-request
                Assert.Equal(LogLevel.Information, testSink.MaxLogLevel);

                // Check for expected message
                Assert.NotNull(testSink.ConnectionResetMessage);
                Assert.Contains("reset", testSink.ConnectionResetMessage);
                Assert.NotNull(testSink.RequestProcessingErrorMessage);
                Assert.Contains("abnormal", testSink.RequestProcessingErrorMessage);
            }
        }

        [Fact]
        public async Task ThrowsOnReadAfterConnectionError()
        {
            var requestStarted = new SemaphoreSlim(0);
            var connectionReset = new SemaphoreSlim(0);
            var appDone = new SemaphoreSlim(0);
            var expectedExceptionThrown = false;

            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(async context =>
                {
                    requestStarted.Release();
                    Assert.True(await connectionReset.WaitAsync(_semaphoreWaitTimeout));

                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                    }
                    catch (IOException ex)
                    {
                        expectedExceptionThrown = ex.InnerException is UvException && ex.InnerException.Message.Contains("ECONNRESET");
                    }

                    appDone.Release();
                }));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.LingerState = new LingerOption(true, 0);
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nContent-Length: 1\r\n\r\n"));
                    Assert.True(await requestStarted.WaitAsync(_semaphoreWaitTimeout));
                }

                connectionReset.Release();

                Assert.True(await appDone.WaitAsync(_semaphoreWaitTimeout));
                Assert.True(expectedExceptionThrown);
            }
        }

        [Fact]
        public async Task RequestAbortedTokenFiredOnClientFIN()
        {
            var appStarted = new SemaphoreSlim(0);
            var requestAborted = new SemaphoreSlim(0);
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(async context =>
                {
                    appStarted.Release();

                    var token = context.RequestAborted;
                    token.Register(() => requestAborted.Release(2));
                    await requestAborted.WaitAsync().TimeoutAfter(TimeSpan.FromSeconds(10));
                }));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n"));
                    await appStarted.WaitAsync();
                    socket.Shutdown(SocketShutdown.Send);
                    await requestAborted.WaitAsync().TimeoutAfter(TimeSpan.FromSeconds(10));
                }
            }
        }

        private async Task TestRemoteIPAddress(string registerAddress, string requestAddress, string expectAddress)
        {
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://{registerAddress}:0")
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var connection = context.Connection;
                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            RemoteIPAddress = connection.RemoteIpAddress?.ToString(),
                            RemotePort = connection.RemotePort,
                            LocalIPAddress = connection.LocalIpAddress?.ToString(),
                            LocalPort = connection.LocalPort
                        }));
                    });
                });

            using (var host = builder.Build())
            using (var client = new HttpClient())
            {
                host.Start();

                var response = await client.GetAsync($"http://{requestAddress}:{host.GetPort()}/");
                response.EnsureSuccessStatusCode();

                var connectionFacts = await response.Content.ReadAsStringAsync();
                Assert.NotEmpty(connectionFacts);

                var facts = JsonConvert.DeserializeObject<JObject>(connectionFacts);
                Assert.Equal(expectAddress, facts["RemoteIPAddress"].Value<string>());
                Assert.NotEmpty(facts["RemotePort"].Value<string>());
            }
        }

        private class ConnectionErrorTestSink : ITestSink
        {
            private readonly Action _connectionStarted;
            private readonly Action _connectionResetLogged;
            private readonly Action _requestProcessingErrorLogged;

            public ConnectionErrorTestSink(
                Action connectionStarted,
                Action connectionResetLogged,
                Action requestProcessingErrorLogged)
            {
                _connectionStarted = connectionStarted;
                _connectionResetLogged = connectionResetLogged;
                _requestProcessingErrorLogged = requestProcessingErrorLogged;
            }

            public LogLevel MaxLogLevel { get; set; }

            public string ConnectionResetMessage { get; set; }

            public string RequestProcessingErrorMessage { get; set; }

            public Func<BeginScopeContext, bool> BeginEnabled { get; set; }

            public List<BeginScopeContext> Scopes { get; set; }

            public Func<WriteContext, bool> WriteEnabled { get; set; }

            public List<WriteContext> Writes { get; set; }

            public void Begin(BeginScopeContext context)
            {
            }

            public void Write(WriteContext context)
            {
                if (context.LoggerName == "Microsoft.AspNetCore.Server.Kestrel" && context.LogLevel > MaxLogLevel)
                {
                    MaxLogLevel = context.LogLevel;
                }

                const int connectionStartEventId = 1;
                const int connectionResetEventId = 19;
                const int requestProcessingErrorEventId = 20;

                if (context.EventId.Id == connectionStartEventId)
                {
                    _connectionStarted();
                }
                else if (context.EventId.Id == connectionResetEventId)
                {
                    ConnectionResetMessage = context.Formatter(context.State, context.Exception);
                    _connectionResetLogged();
                }
                else if (context.EventId.Id == requestProcessingErrorEventId)
                {
                    RequestProcessingErrorMessage = context.Formatter(context.State, context.Exception);
                    _requestProcessingErrorLogged();
                }
            }
        }
    }
}