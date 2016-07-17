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
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Testing.xunit;
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
        public async Task ConnectionResetAbortsRequest()
        {
            var connectionStarted = new SemaphoreSlim(0);
            var connectionErrorLogged = new SemaphoreSlim(0);
            var testSink = new ConnectionErrorTestSink(
                () => connectionStarted.Release(), () => connectionErrorLogged.Release());
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
                Assert.True(await connectionErrorLogged.WaitAsync(_semaphoreWaitTimeout));

                // Check for expected message
                Assert.NotNull(testSink.ConnectionErrorMessage);
                Assert.Contains("ECONNRESET", testSink.ConnectionErrorMessage);
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
                    catch (BadHttpRequestException)
                    {
                        // We need this here because BadHttpRequestException derives from IOException,
                        // and we're looking for an actual IOException.
                    }
                    catch (IOException ex)
                    {
                        // This is one of two exception types that might thrown in this scenario.
                        // An IOException is thrown if ReadAsync is awaiting on SocketInput when
                        // the connection is aborted.
                        expectedExceptionThrown = ex.InnerException is UvException;
                    }
                    catch (TaskCanceledException)
                    {
                        // This is the other exception type that might be thrown here.
                        // A TaskCanceledException is thrown if ReadAsync is called when the
                        // connection has already been aborted, since FrameRequestStream is
                        // aborted and returns a canceled task from ReadAsync.
                        expectedExceptionThrown = true;
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
            private readonly Action _connectionErrorLogged;

            public ConnectionErrorTestSink(Action connectionStarted, Action connectionErrorLogged)
            {
                _connectionStarted = connectionStarted;
                _connectionErrorLogged = connectionErrorLogged;
            }

            public string ConnectionErrorMessage { get; set; }

            public Func<BeginScopeContext, bool> BeginEnabled { get; set; }

            public List<BeginScopeContext> Scopes { get; set; }

            public Func<WriteContext, bool> WriteEnabled { get; set; }

            public List<WriteContext> Writes { get; set; }

            public void Begin(BeginScopeContext context)
            {
            }

            public void Write(WriteContext context)
            {
                const int connectionStartEventId = 1;
                const int connectionErrorEventId = 14;

                if (context.EventId.Id == connectionStartEventId)
                {
                    _connectionStarted();
                }
                else if (context.EventId.Id == connectionErrorEventId)
                {
                    ConnectionErrorMessage = context.Exception?.Message;
                    _connectionErrorLogged();
                }
            }
        }
    }
}