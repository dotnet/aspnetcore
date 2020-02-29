// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestTests : LoggedTest
    {
        private const int _connectionStartedEventId = 1;
        private const int _connectionResetEventId = 19;
        private static readonly int _semaphoreWaitTimeout = Debugger.IsAttached ? 10000 : 2500;

        public static TheoryData<ListenOptions> ConnectionMiddlewareData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)).UsePassThrough()
        };

        [Theory]
        [InlineData(10 * 1024 * 1024, true)]
        // In the following dataset, send at least 2GB.
        // Never change to a lower value, otherwise regression testing for
        // https://github.com/aspnet/KestrelHttpServer/issues/520#issuecomment-188591242
        // will be lost.
        [InlineData((long)int.MaxValue + 1, false)]
        public async Task LargeUpload(long contentLength, bool checkBytes)
        {
            const int bufferLength = 1024 * 1024;
            Assert.True(contentLength % bufferLength == 0, $"{nameof(contentLength)} sent must be evenly divisible by {bufferLength}.");
            Assert.True(bufferLength % 256 == 0, $"{nameof(bufferLength)} must be evenly divisible by 256");

            var builder = TransportSelector.GetWebHostBuilder()
                .ConfigureServices(AddTestLogging)
                .UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = contentLength;
                    options.Limits.MinRequestBodyDataRate = null;
                })
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
                                    // Do not use Assert.Equal here, it is to slow for this hot path
                                    Assert.True((byte)((total + i) % 256) == receivedBytes[i], "Data received is incorrect");
                                }
                            }

                            total += received;
                        }

                        await context.Response.WriteAsync($"bytesRead: {total.ToString()}");
                    });
                });

            using (var host = builder.Build())
            {
                await host.StartAsync();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("POST / HTTP/1.1\r\nHost: \r\n"));
                    socket.Send(Encoding.ASCII.GetBytes($"Content-Length: {contentLength}\r\n\r\n"));

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

                    using (var stream = new NetworkStream(socket))
                    {
                        await AssertStreamContains(stream, $"bytesRead: {contentLength}");
                    }
                }

                await host.StopAsync();
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
            var builder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .ConfigureServices(AddTestLogging)
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
                await host.StartAsync();

                client.DefaultRequestHeaders.Connection.Clear();
                client.DefaultRequestHeaders.Connection.Add("close");

                var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
                response.EnsureSuccessStatusCode();

                await host.StopAsync();
            }
        }

        [Fact]
        public async Task CanHandleMultipleConcurrentRequests()
        {
            var requestNumber = 0;
            var ensureConcurrentRequestTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                if (Interlocked.Increment(ref requestNumber) == 1)
                {
                    await ensureConcurrentRequestTcs.Task.DefaultTimeout();
                }
                else
                {
                    ensureConcurrentRequestTcs.SetResult(null);
                }
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection1 = server.CreateConnection())
                using (var connection2 = server.CreateConnection())
                {
                    await connection1.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection2.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    await connection1.Receive($"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                    await connection2.Receive($"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConnectionResetPriorToRequestIsLoggedAsDebug()
        {
            var connectionStarted = new SemaphoreSlim(0);
            var connectionReset = new SemaphoreSlim(0);
            var loggedHigherThanDebug = false;

            TestSink.MessageLogged += context =>
            {
                if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
                {
                    return;
                }

                if (context.EventId.Id == _connectionStartedEventId)
                {
                    connectionStarted.Release();
                }
                else if (context.EventId.Id == _connectionResetEventId)
                {
                    connectionReset.Release();
                }

                if (context.LogLevel > LogLevel.Debug)
                {
                    loggedHigherThanDebug = true;
                }
            };

            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    // Wait until connection is established
                    Assert.True(await connectionStarted.WaitAsync(TestConstants.DefaultTimeout));

                    connection.Reset();
                }

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                Assert.True(await connectionReset.WaitAsync(TestConstants.DefaultTimeout));

                await server.StopAsync();
            }

            Assert.False(loggedHigherThanDebug);
        }

        [Fact]
        public async Task ConnectionResetBetweenRequestsIsLoggedAsDebug()
        {
            var connectionReset = new SemaphoreSlim(0);
            var loggedHigherThanDebug = false;

            TestSink.MessageLogged += context =>
            {
                if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
                {
                    return;
                }

                if (context.LogLevel > LogLevel.Debug)
                {
                    loggedHigherThanDebug = true;
                }

                if (context.EventId.Id == _connectionResetEventId)
                {
                    connectionReset.Release();
                }
            };

            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    // Make sure the response is fully received, so a write failure (e.g. EPIPE) doesn't cause
                    // a more critical log message.
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");

                    connection.Reset();
                    // Force a reset
                }

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                Assert.True(await connectionReset.WaitAsync(TestConstants.DefaultTimeout));
                await server.StopAsync();
            }

            Assert.False(loggedHigherThanDebug);
        }

        [Fact]
        public async Task ConnectionResetMidRequestIsLoggedAsDebug()
        {
            var requestStarted = new SemaphoreSlim(0);
            var connectionReset = new SemaphoreSlim(0);
            var connectionClosing = new SemaphoreSlim(0);
            var loggedHigherThanDebug = false;

            TestSink.MessageLogged += context =>
            {
                if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
                {
                    return;
                }

                if (context.LogLevel > LogLevel.Debug)
                {
                    loggedHigherThanDebug = true;
                }

                if (context.EventId.Id == _connectionResetEventId)
                {
                    connectionReset.Release();
                }
            };

            using (var server = new TestServer(async context =>
                {
                    requestStarted.Release();
                    await connectionClosing.WaitAsync();
                },
                new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGet();

                    // Wait until connection is established
                    Assert.True(await requestStarted.WaitAsync(TestConstants.DefaultTimeout), "request should have started");

                    connection.Reset();
                }

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                Assert.True(await connectionReset.WaitAsync(TestConstants.DefaultTimeout), "Connection reset event should have been logged");
                connectionClosing.Release();
                await server.StopAsync();
            }

            Assert.False(loggedHigherThanDebug, "Logged event should not have been higher than debug.");
        }

        [Fact]
        public async Task ThrowsOnReadAfterConnectionError()
        {
            var requestStarted = new SemaphoreSlim(0);
            var connectionReset = new SemaphoreSlim(0);
            var appDone = new SemaphoreSlim(0);
            var expectedExceptionThrown = false;

            var builder = TransportSelector.GetWebHostBuilder()
                .ConfigureServices(AddTestLogging)
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .Configure(app => app.Run(async context =>
                {
                    requestStarted.Release();
                    Assert.True(await connectionReset.WaitAsync(_semaphoreWaitTimeout));

                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                    }
                    catch (ConnectionResetException)
                    {
                        expectedExceptionThrown = true;
                    }

                    appDone.Release();
                }));

            using (var host = builder.Build())
            {
                await host.StartAsync();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.LingerState = new LingerOption(true, 0);
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\nContent-Length: 1\r\n\r\n"));
                    Assert.True(await requestStarted.WaitAsync(_semaphoreWaitTimeout));
                }

                connectionReset.Release();

                Assert.True(await appDone.WaitAsync(_semaphoreWaitTimeout));
                Assert.True(expectedExceptionThrown);

                await host.StopAsync();
            }
        }

        [Fact]
        public async Task RequestAbortedTokenFiredOnClientFIN()
        {
            var appStarted = new SemaphoreSlim(0);
            var requestAborted = new SemaphoreSlim(0);
            var builder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .ConfigureServices(AddTestLogging)
                .Configure(app => app.Run(async context =>
                {
                    appStarted.Release();

                    var token = context.RequestAborted;
                    token.Register(() => requestAborted.Release(2));
                    await requestAborted.WaitAsync().DefaultTimeout();
                }));

            using (var host = builder.Build())
            {
                await host.StartAsync();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n"));
                    await appStarted.WaitAsync();
                    socket.Shutdown(SocketShutdown.Send);
                    await requestAborted.WaitAsync().DefaultTimeout();
                }

                await host.StopAsync();
            }
        }

        [Fact]
        public async Task AbortingTheConnectionSendsFIN()
        {
            var builder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0")
                .ConfigureServices(AddTestLogging)
                .Configure(app => app.Run(context =>
                {
                    context.Abort();
                    return Task.CompletedTask;
                }));

            using (var host = builder.Build())
            {
                await host.StartAsync();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n"));
                    int result = socket.Receive(new byte[32]);
                    Assert.Equal(0, result);
                }

                await host.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionMiddlewareData))]
        public async Task ConnectionClosedTokenFiresOnClientFIN(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var appStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(context =>
            {
                appStartedTcs.SetResult(null);

                var connectionLifetimeFeature = context.Features.Get<IConnectionLifetimeFeature>();
                connectionLifetimeFeature.ConnectionClosed.Register(() => connectionClosedTcs.SetResult(null));

                return Task.CompletedTask;
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    await appStartedTcs.Task.DefaultTimeout();

                    connection.Shutdown(SocketShutdown.Send);

                    await connectionClosedTcs.Task.DefaultTimeout();
                }
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionMiddlewareData))]
        public async Task ConnectionClosedTokenFiresOnServerFIN(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var connectionClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(context =>
            {
                var connectionLifetimeFeature = context.Features.Get<IConnectionLifetimeFeature>();
                connectionLifetimeFeature.ConnectionClosed.Register(() => connectionClosedTcs.SetResult(null));

                return Task.CompletedTask;
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: close",
                        "",
                        "");

                    await connectionClosedTcs.Task.DefaultTimeout();

                    await connection.ReceiveEnd($"HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionMiddlewareData))]
        public async Task ConnectionClosedTokenFiresOnServerAbort(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var connectionClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(context =>
            {
                var connectionLifetimeFeature = context.Features.Get<IConnectionLifetimeFeature>();
                connectionLifetimeFeature.ConnectionClosed.Register(() => connectionClosedTcs.SetResult(null));

                context.Abort();

                return Task.CompletedTask;
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    await connectionClosedTcs.Task.DefaultTimeout();

                    try
                    {
                        await connection.ReceiveEnd();
                    }
                    catch (IOException)
                    {
                        // The server is forcefully closing the connection so an IOException:
                        // "Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."
                        // isn't guaranteed but not unexpected.
                    }
                }
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionMiddlewareData))]
        public async Task RequestsCanBeAbortedMidRead(ListenOptions listenOptions)
        {
            // This needs a timeout.
            const int applicationAbortedConnectionId = 34;

            var testContext = new TestServiceContext(LoggerFactory);

            var readTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registrationTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestId = 0;

            using (var server = new TestServer(async httpContext =>
            {
                requestId++;

                var response = httpContext.Response;
                var request = httpContext.Request;
                var lifetime = httpContext.Features.Get<IHttpRequestLifetimeFeature>();

                lifetime.RequestAborted.Register(() => registrationTcs.TrySetResult(requestId));

                if (requestId == 1)
                {
                    response.Headers["Content-Length"] = new[] { "5" };

                    await response.WriteAsync("World");
                }
                else
                {
                    var readTask = request.Body.CopyToAsync(Stream.Null);

                    lifetime.Abort();

                    try
                    {
                        await readTask;
                    }
                    catch (Exception ex)
                    {
                        readTcs.SetException(ex);
                        throw;
                    }
                    finally
                    {
                        await registrationTcs.Task.DefaultTimeout();
                    }

                    readTcs.SetException(new Exception("This shouldn't be reached."));
                }
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // Full request and response
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "Hello");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 5",
                        "",
                        "World");

                    // Never send the body so CopyToAsync always fails.
                    await connection.Send("POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 5",
                        "",
                        "");
                    await connection.WaitForConnectionClose();
                }
                await server.StopAsync();
            }

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await readTcs.Task);

            // The cancellation token for only the last request should be triggered.
            var abortedRequestId = await registrationTcs.Task.DefaultTimeout();
            Assert.Equal(2, abortedRequestId);

            Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel" &&
                                                     w.EventId == applicationAbortedConnectionId));
        }

        [Theory]
        [MemberData(nameof(ConnectionMiddlewareData))]
        public async Task ServerCanAbortConnectionAfterUnobservedClose(ListenOptions listenOptions)
        {
            const int connectionPausedEventId = 4;
            const int connectionFinSentEventId = 7;
            const int maxRequestBufferSize = 4096;

            var readCallbackUnwired = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientClosedConnection = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serverClosedConnection = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appFuncCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            TestSink.MessageLogged += context =>
            {
                if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" &&
                    context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
                {
                    return;
                }

                if (context.EventId.Id == connectionPausedEventId)
                {
                    readCallbackUnwired.TrySetResult(null);
                }
                else if (context.EventId == connectionFinSentEventId)
                {
                    serverClosedConnection.SetResult(null);
                }
            };

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)
            {
                ServerOptions =
                {
                    Limits =
                    {
                        MaxRequestBufferSize = maxRequestBufferSize,
                        MaxRequestLineSize = maxRequestBufferSize,
                        MaxRequestHeadersTotalSize = maxRequestBufferSize,
                    }
                }
            };

            var scratchBuffer = new byte[maxRequestBufferSize * 8];

            using (var server = new TestServer(async context =>
            {
                await clientClosedConnection.Task;

                context.Abort();

                await serverClosedConnection.Task;

                appFuncCompleted.SetResult(null);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        $"Content-Length: {scratchBuffer.Length}",
                        "",
                        "");

                    var ignore = connection.Stream.WriteAsync(scratchBuffer, 0, scratchBuffer.Length);

                    // Wait until the read callback is no longer hooked up so that the connection disconnect isn't observed.
                    await readCallbackUnwired.Task.DefaultTimeout();
                }

                clientClosedConnection.SetResult(null);

                await appFuncCompleted.Task.DefaultTimeout();
                await server.StopAsync();
            }

            mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.Once());
        }

        [Theory]
        [MemberData(nameof(ConnectionMiddlewareData))]
        public async Task AppCanHandleClientAbortingConnectionMidRequest(ListenOptions listenOptions)
        {
            var readTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object);

            var scratchBuffer = new byte[4096];

            using (var server = new TestServer(async context =>
            {
                appStartedTcs.SetResult(null);

                try
                {
                    await context.Request.Body.CopyToAsync(Stream.Null); ;
                }
                catch (Exception ex)
                {
                    readTcs.SetException(ex);
                    throw;
                }

                readTcs.SetException(new Exception("This shouldn't be reached."));

            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        $"Content-Length: {scratchBuffer.Length * 2}",
                        "",
                        "");

                    await appStartedTcs.Task.DefaultTimeout();

                    await connection.Stream.WriteAsync(scratchBuffer, 0, scratchBuffer.Length);

                    connection.Reset();
                }

                await Assert.ThrowsAnyAsync<IOException>(() => readTcs.Task).DefaultTimeout();
                await server.StopAsync();
            }

            mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.Once());
        }

        private async Task TestRemoteIPAddress(string registerAddress, string requestAddress, string expectAddress)
        {
            var builder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://{registerAddress}:0")
                .ConfigureServices(AddTestLogging)
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
                await host.StartAsync();

                var response = await client.GetAsync($"http://{requestAddress}:{host.GetPort()}/");
                response.EnsureSuccessStatusCode();

                var connectionFacts = await response.Content.ReadAsStringAsync();
                Assert.NotEmpty(connectionFacts);

                var facts = JsonConvert.DeserializeObject<JObject>(connectionFacts);
                Assert.Equal(expectAddress, facts["RemoteIPAddress"].Value<string>());
                Assert.NotEmpty(facts["RemotePort"].Value<string>());

                await host.StopAsync();
            }
        }

        // THIS IS NOT GENERAL PURPOSE. If the initial characters could repeat, this is broken. However, since we're
        // looking for /bytesWritten: \d+/ and the initial "b" cannot occur elsewhere in the pattern, this works.
        private static async Task AssertStreamContains(Stream stream, string expectedSubstring)
        {
            var expectedBytes = Encoding.ASCII.GetBytes(expectedSubstring);
            var exptectedLength = expectedBytes.Length;
            var responseBuffer = new byte[exptectedLength];

            var matchedChars = 0;

            while (matchedChars < exptectedLength)
            {
                var count = await stream.ReadAsync(responseBuffer, 0, exptectedLength - matchedChars).DefaultTimeout();

                if (count == 0)
                {
                    Assert.True(false, "Stream completed without expected substring.");
                }

                for (var i = 0; i < count && matchedChars < exptectedLength; i++)
                {
                    if (responseBuffer[i] == expectedBytes[matchedChars])
                    {
                        matchedChars++;
                    }
                    else
                    {
                        matchedChars = 0;
                    }
                }
            }
        }
    }
}
