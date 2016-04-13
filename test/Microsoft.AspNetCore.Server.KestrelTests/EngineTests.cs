// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    /// <summary>
    /// Summary description for EngineTests
    /// </summary>
    public class EngineTests
    {
        public static TheoryData<ServiceContext> ConnectionFilterData
        {
            get
            {
                return new TheoryData<ServiceContext>
                {
                    {
                        new TestServiceContext()
                    },
                    {
                        new TestServiceContext(new PassThroughConnectionFilter())
                    }
                };
            }
        }

        private async Task App(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            response.Headers.Clear();
            while (true)
            {
                var buffer = new byte[8192];
                var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await response.Body.WriteAsync(buffer, 0, count);
            }
        }

        private async Task AppChunked(HttpContext httpContext)
        {
            var request = httpContext.Request;
            var response = httpContext.Response;
            var data = new MemoryStream();
            await request.Body.CopyToAsync(data);
            var bytes = data.ToArray();

            response.Headers.Clear();
            response.Headers["Content-Length"] = bytes.Length.ToString();
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        private Task EmptyApp(HttpContext httpContext)
        {
            httpContext.Response.Headers.Clear();
            return Task.FromResult<object>(null);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void EngineCanStartAndStop(ServiceContext testContext)
        {
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            engine.Dispose();
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void ListenerCanCreateAndDispose(TestServiceContext testContext)
        {
            testContext.App = App;
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            var address = ServerAddress.FromUrl($"http://localhost:{TestServer.GetNextPort()}/");
            var started = engine.CreateServer(address);
            started.Dispose();
            engine.Dispose();
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void ConnectionCanReadAndWrite(TestServiceContext testContext)
        {
            var port = TestServer.GetNextPort();
            testContext.App = App;
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            var address = ServerAddress.FromUrl($"http://localhost:{port}/");
            var started = engine.CreateServer(address);

            var socket = TestConnection.CreateConnectedLoopbackSocket(port);
            socket.Send(Encoding.ASCII.GetBytes("POST / HTTP/1.0\r\n\r\nHello World"));
            socket.Shutdown(SocketShutdown.Send);
            var buffer = new byte[8192];
            while (true)
            {
                var length = socket.Receive(buffer);
                if (length == 0) { break; }
                var text = Encoding.ASCII.GetString(buffer, 0, length);
            }
            started.Dispose();
            engine.Dispose();
        }


        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "",
                        "Hello World");
                }
            }
        }


        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http11(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "Goodbye");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ReuseStreamsOn(ServiceContext testContext)
        {
            testContext.ServerOptions.MaxPooledStreams = 120;

            var streamCount = 0;
            var loopCount = 20;
            Stream lastStream = null;

            using (var server = new TestServer(
                context =>
                    {
                        if (context.Request.Body != lastStream)
                        {
                            lastStream = context.Request.Body;
                            streamCount++;
                        }
                        context.Response.Headers.Clear();
                        return context.Request.Body.CopyToAsync(context.Response.Body);
                    },
                    testContext))
            {

                using (var connection = new TestConnection(server.Port))
                {
                    var requestData =
                        Enumerable.Repeat("GET / HTTP/1.1\r\n", loopCount)
                            .Concat(new[] { "GET / HTTP/1.1\r\nConnection: close\r\n\r\nGoodbye" });

                    var responseData =
                        Enumerable.Repeat("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n", loopCount)
                            .Concat(new[] { "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\nGoodbye" });

                    await connection.SendEnd(requestData.ToArray());

                    await connection.ReceiveEnd(responseData.ToArray());
                }

                Assert.Equal(1, streamCount);
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ReuseStreamsOff(ServiceContext testContext)
        {
            testContext.ServerOptions.MaxPooledStreams = 0;

            var streamCount = 0;
            var loopCount = 20;
            Stream lastStream = null;

            using (var server = new TestServer(
                context =>
                {
                    if (context.Request.Body != lastStream)
                    {
                        lastStream = context.Request.Body;
                        streamCount++;
                    }
                    context.Response.Headers.Clear();
                    return context.Request.Body.CopyToAsync(context.Response.Body);
                },
                    testContext))
            {

                using (var connection = new TestConnection(server.Port))
                {
                    var requestData =
                        Enumerable.Repeat("GET / HTTP/1.1\r\n", loopCount)
                            .Concat(new[] { "GET / HTTP/1.1\r\nConnection: close\r\n\r\nGoodbye" });

                    var responseData =
                        Enumerable.Repeat("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n", loopCount)
                            .Concat(new[] { "HTTP/1.1 200 OK\r\nConnection: close\r\n\r\nGoodbye" });

                    await connection.SendEnd(requestData.ToArray());

                    await connection.ReceiveEnd(responseData.ToArray());
                }

                Assert.Equal(loopCount + 1, streamCount);
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10ContentLength(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAlive(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "POST / HTTP/1.0",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Connection: keep-alive",
                        "Content-Length: 0",
                        "\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAliveNotUsedIfResponseContentLengthNotSet(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "POST / HTTP/1.0",
                        "Content-Length: 7",
                        "Connection: keep-alive",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Connection: keep-alive",
                        "Content-Length: 0",
                        "\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAliveContentLength(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "Connection: keep-alive",
                        "",
                        "Hello WorldPOST / HTTP/1.0",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Connection: keep-alive",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Expect100ContinueForBody(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Expect: 100-continue",
                        "Connection: close",
                        "Content-Length: 11",
                        "\r\n");
                    await connection.Receive("HTTP/1.1 100 Continue", "\r\n");
                    await connection.SendEnd("Hello World");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task DisconnectingClient(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                var socket = TestConnection.CreateConnectedLoopbackSocket(server.Port);
                await Task.Delay(200);
                socket.Dispose();

                await Task.Delay(200);
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.0",
                        "\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "\r\n");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthSetAutomaticallyAfterNoWrites(ServiceContext testContext)
        {
            using (var server = new TestServer(EmptyApp, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.0 200 OK",
                        "Connection: keep-alive",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForNonKeepAliveRequests(ServiceContext testContext)
        {
            using (var server = new TestServer(EmptyApp, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        "",
                        "");
                }

                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.0 200 OK",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForHeadRequests(ServiceContext testContext)
        {
            using (var server = new TestServer(EmptyApp, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "HEAD / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes(ServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var request = httpContext.Request;
                var response = httpContext.Response;
                response.Headers.Clear();

                using (var reader = new StreamReader(request.Body, Encoding.ASCII))
                {
                    var statusString = await reader.ReadLineAsync();
                    response.StatusCode = int.Parse(statusString);
                }
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.1",
                        "Content-Length: 3",
                        "",
                        "101POST / HTTP/1.1",
                        "Content-Length: 3",
                        "",
                        "204POST / HTTP/1.1",
                        "Content-Length: 3",
                        "",
                        "205POST / HTTP/1.1",
                        "Content-Length: 3",
                        "",
                        "304POST / HTTP/1.1",
                        "Content-Length: 3",
                        "",
                        "200");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "",
                        "HTTP/1.1 204 No Content",
                        "",
                        "HTTP/1.1 205 Reset Content",
                        "",
                        "HTTP/1.1 304 Not Modified",
                        "",
                        "HTTP/1.1 200 OK",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingResultsIn500Response(ServiceContext testContext)
        {
            bool onStartingCalled = false;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                // Anything added to the ResponseHeaders dictionary is ignored
                response.Headers.Clear();
                response.Headers["Content-Length"] = "11";
                throw new Exception();
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 500 Internal Server Error",
                        "");
                    await connection.ReceiveStartsWith("Date:");
                    await connection.Receive(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "",
                        "HTTP/1.1 500 Internal Server Error",
                        "");
                    await connection.Receive("Connection: close",
                        "");
                    await connection.ReceiveStartsWith("Date:");
                    await connection.ReceiveEnd(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "",
                        "");

                    Assert.False(onStartingCalled);
                    Assert.Equal(2, testLogger.ApplicationErrorsLogged);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingAfterWritingKillsConnection(ServiceContext testContext)
        {
            bool onStartingCalled = false;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
                throw new Exception();
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello World");

                    Assert.True(onStartingCalled);
                    Assert.Equal(1, testLogger.ApplicationErrorsLogged);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingAfterPartialWriteKillsConnection(ServiceContext testContext)
        {
            bool onStartingCalled = false;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello"), 0, 5);
                throw new Exception();
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello");

                    Assert.True(onStartingCalled);
                    Assert.Equal(1, testLogger.ApplicationErrorsLogged);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ConnectionClosesWhenFinReceived(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "Post / HTTP/1.1",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ConnectionClosesWhenFinReceivedBeforeRequestCompletes(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET /");
                    await connection.ReceiveEnd();
                }

                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "Post / HTTP/1.1");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "Post / HTTP/1.1",
                        "Content-Length: 7");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingInOnStartingResultsInFailedWritesAnd500Response(ServiceContext testContext)
        {
            var onStartingCallCount1 = 0;
            var onStartingCallCount2 = 0;
            var failedWriteCount = 0;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(async httpContext =>
            {
                var onStartingException = new Exception();

                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCallCount1++;
                    throw onStartingException;
                }, null);
                response.OnStarting(_ =>
                {
                    onStartingCallCount2++;
                    throw onStartingException;
                }, null);

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                var writeException = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

                Assert.Same(onStartingException, writeException.InnerException);

                failedWriteCount++;
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 500 Internal Server Error",
                        "");
                    await connection.ReceiveStartsWith("Date:");
                    await connection.Receive(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "",
                        "HTTP/1.1 500 Internal Server Error",
                        "Connection: close",
                        "");
                    await connection.ReceiveStartsWith("Date:");
                    await connection.ReceiveEnd(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "",
                        "");

                    Assert.Equal(2, onStartingCallCount1);
                    // The second OnStarting callback should not be called since the first failed.
                    Assert.Equal(0, onStartingCallCount2);
                    Assert.Equal(2, testLogger.ApplicationErrorsLogged);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingInOnCompletedIsLoggedAndClosesConnection(ServiceContext testContext)
        {
            var onCompletedCalled1 = false;
            var onCompletedCalled2 = false;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnCompleted(_ =>
                {
                    onCompletedCalled1 = true;
                    throw new Exception();
                }, null);
                response.OnCompleted(_ =>
                {
                    onCompletedCalled2 = true;
                    throw new Exception();
                }, null);

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }

                // All OnCompleted callbacks should be called even if they throw.
                Assert.Equal(2, testLogger.ApplicationErrorsLogged);
                Assert.True(onCompletedCalled1);
                Assert.True(onCompletedCalled2);
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task RequestsCanBeAbortedMidRead(ServiceContext testContext)
        {
            var readTcs = new TaskCompletionSource<object>();
            var registrationTcs = new TaskCompletionSource<int>();
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
                    response.Headers.Clear();
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

                    readTcs.SetException(new Exception("This shouldn't be reached."));
                }
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    // Never send the body so CopyToAsync always fails.
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Content-Length: 5",
                        "",
                        "HelloPOST / HTTP/1.1",
                        "Content-Length: 5",
                        "",
                        "");

                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 5",
                        "",
                        "World");
                }
            }

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await readTcs.Task);

            // The cancellation token for only the last request should be triggered.
            var abortedRequestId = await registrationTcs.Task;
            Assert.Equal(2, abortedRequestId);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task FailedWritesResultInAbortedRequest(ServiceContext testContext)
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;
            // Ensure string is long enough to disable write-behind buffering
            var largeString = new string('a', maxBytesPreCompleted + 1);

            var writeTcs = new TaskCompletionSource<object>();
            var registrationWh = new ManualResetEventSlim();
            var connectionCloseWh = new ManualResetEventSlim();

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                var request = httpContext.Request;
                var lifetime = httpContext.Features.Get<IHttpRequestLifetimeFeature>();

                lifetime.RequestAborted.Register(() => registrationWh.Set());

                await request.Body.CopyToAsync(Stream.Null);
                connectionCloseWh.Wait();

                response.Headers.Clear();

                try
                {
                    // Ensure write is long enough to disable write-behind buffering
                    for (int i = 0; i < 100; i++)
                    {
                        await response.WriteAsync(largeString, lifetime.RequestAborted);
                        registrationWh.Wait(1000);
                    }
                }
                catch (Exception ex)
                {
                    writeTcs.SetException(ex);
                    throw;
                }

                writeTcs.SetException(new Exception("This shouldn't be reached."));
            }, testContext))
            {
                using (var connection = new TestConnection(server.Port))
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Content-Length: 5",
                        "",
                        "Hello");
                    // Don't wait to receive the response. Just close the socket.
                }

                connectionCloseWh.Set();

                // Write failed
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await writeTcs.Task);
                // RequestAborted tripped
                Assert.True(registrationWh.Wait(1000));
            }
        }
    }
}
