// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    /// <summary>
    /// Summary description for EngineTests
    /// </summary>
    public class EngineTests
    {
        public static TheoryData<TestServiceContext> ConnectionFilterData
        {
            get
            {
                return new TheoryData<TestServiceContext>
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

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void EngineCanStartAndStop(TestServiceContext testContext)
        {
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            engine.Dispose();
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void ListenerCanCreateAndDispose(TestServiceContext testContext)
        {
            testContext.App = TestApp.EchoApp;
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            var address = ServerAddress.FromUrl("http://127.0.0.1:0/");
            var started = engine.CreateServer(address);
            started.Dispose();
            engine.Dispose();
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void ConnectionCanReadAndWrite(TestServiceContext testContext)
        {
            testContext.App = TestApp.EchoApp;
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            var address = ServerAddress.FromUrl("http://127.0.0.1:0/");
            var started = engine.CreateServer(address);

            var socket = TestConnection.CreateConnectedLoopbackSocket(address.Port);
            var data = "Hello World";
            socket.Send(Encoding.ASCII.GetBytes($"POST / HTTP/1.0\r\nContent-Length: 11\r\n\r\n{data}"));
            var buffer = new byte[data.Length];
            var read = 0;
            while (read < data.Length)
            {
                read += socket.Receive(buffer, read, buffer.Length - read, SocketFlags.None);
            }
            socket.Dispose();
            started.Dispose();
            engine.Dispose();
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10RequestReceivesHttp11Response(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http11(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task HeadersAndStreamsAreReused(TestServiceContext testContext)
        {
            var streamCount = 0;
            var requestHeadersCount = 0;
            var responseHeadersCount = 0;
            var loopCount = 20;
            Stream lastStream = null;
            IHeaderDictionary lastRequestHeaders = null;
            IHeaderDictionary lastResponseHeaders = null;

            using (var server = new TestServer(
                async context =>
                    {
                        if (context.Request.Body != lastStream)
                        {
                            lastStream = context.Request.Body;
                            streamCount++;
                        }
                        if (context.Request.Headers != lastRequestHeaders)
                        {
                            lastRequestHeaders = context.Request.Headers;
                            requestHeadersCount++;
                        }
                        if (context.Response.Headers != lastResponseHeaders)
                        {
                            lastResponseHeaders = context.Response.Headers;
                            responseHeadersCount++;
                        }

                        var ms = new MemoryStream();
                        await context.Request.Body.CopyToAsync(ms);
                        var request = ms.ToArray();

                        context.Response.ContentLength = request.Length;

                        await context.Response.Body.WriteAsync(request, 0, request.Length);
                    },
                    testContext))
            {

                using (var connection = server.CreateConnection())
                {
                    var requestData =
                        Enumerable.Repeat("GET / HTTP/1.1\r\n", loopCount)
                            .Concat(new[] { "GET / HTTP/1.1\r\nContent-Length: 7\r\nConnection: close\r\n\r\nGoodbye" });

                    var response = string.Join("\r\n", new string[] {
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        ""});

                    var lastResponse = string.Join("\r\n", new string[]
                    {
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye"
                    });

                    var responseData =
                        Enumerable.Repeat(response, loopCount)
                            .Concat(new[] { lastResponse });

                    await connection.Send(requestData.ToArray());

                    await connection.ReceiveEnd(responseData.ToArray());
                }

                Assert.Equal(1, streamCount);
                Assert.Equal(1, requestHeadersCount);
                Assert.Equal(1, responseHeadersCount);
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10ContentLength(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAlive(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "POST / HTTP/1.0",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAliveNotUsedIfResponseContentLengthNotSet(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "\r\n");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10KeepAliveContentLength(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "Connection: keep-alive",
                        "",
                        "Hello WorldPOST / HTTP/1.0",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Expect100ContinueForBody(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Expect: 100-continue",
                        "Connection: close",
                        "Content-Length: 11",
                        "\r\n");
                    await connection.Receive(
                        "HTTP/1.1 100 Continue",
                        "",
                        "");
                    await connection.Send("Hello World");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task DisconnectingClient(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoApp, testContext))
            {
                var socket = TestConnection.CreateConnectedLoopbackSocket(server.Port);
                await Task.Delay(200);
                socket.Dispose();

                await Task.Delay(200);
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthSetAutomaticallyAfterNoWrites(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EmptyApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthSetAutomaticallyForNonKeepAliveRequests(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).TimeoutAfter(TimeSpan.FromSeconds(10)));
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForHeadRequests(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EmptyApp, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var request = httpContext.Request;
                var response = httpContext.Response;

                using (var reader = new StreamReader(request.Body, Encoding.ASCII))
                {
                    var statusString = await reader.ReadLineAsync();
                    response.StatusCode = int.Parse(statusString);
                }
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
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
                        "HTTP/1.1 204 No Content",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "HTTP/1.1 205 Reset Content",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "HTTP/1.1 304 Not Modified",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ZeroContentLengthAssumedOnNonKeepAliveRequestsWithoutContentLengthOrTransferEncodingHeader(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                // This will hang if 0 content length is not assumed by the server
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).TimeoutAfter(TimeSpan.FromSeconds(10)));
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    // Use Send instead of SendEnd to ensure the connection will remain open while
                    // the app runs and reads 0 bytes from the body nonetheless. This checks that
                    // https://github.com/aspnet/KestrelHttpServer/issues/1104 is not regressing.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ConnectionClosedAfter101Response(TestServiceContext testContext)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var request = httpContext.Request;
                var stream = await httpContext.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                var response = Encoding.ASCII.GetBytes("hello, world");
                await stream.WriteAsync(response, 0, response.Length);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "hello, world");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "hello, world");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingResultsIn500Response(TestServiceContext testContext)
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
                    return TaskCache.CompletedTask;
                }, null);

                // Anything added to the ResponseHeaders dictionary is ignored
                response.Headers["Content-Length"] = "11";
                throw new Exception();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "Connection: close",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 500 Internal Server Error",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 500 Internal Server Error",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.False(onStartingCalled);
            Assert.Equal(2, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingAfterWritingKillsConnection(TestServiceContext testContext)
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

                response.Headers["Content-Length"] = new[] { "11" };
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
                throw new Exception();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }

            Assert.True(onStartingCalled);
            Assert.Equal(1, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingAfterPartialWriteKillsConnection(TestServiceContext testContext)
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

                response.Headers["Content-Length"] = new[] { "11" };
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello"), 0, 5);
                throw new Exception();
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello");
                }
            }

            Assert.True(onStartingCalled);
            Assert.Equal(1, testLogger.ApplicationErrorsLogged);
        }
        
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ConnectionClosesWhenFinReceivedBeforeRequestCompletes(TestServiceContext testContext)
        {
            using (var server = new TestServer(TestApp.EchoAppChunked, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "POST / HTTP/1.1");
                    connection.Shutdown(SocketShutdown.Send);
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 400 Bad Request",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "POST / HTTP/1.1",
                        "Content-Length: 7");
                    connection.Shutdown(SocketShutdown.Send);
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 400 Bad Request",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingInOnStartingResultsInFailedWritesAnd500Response(TestServiceContext testContext)
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

                response.Headers["Content-Length"] = new[] { "11" };

                var writeException = await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

                Assert.Same(onStartingException, writeException.InnerException);

                failedWriteCount++;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 500 Internal Server Error",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 500 Internal Server Error",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            // The first registered OnStarting callback should not be called,
            // since they are called LIFO and the other one failed.
            Assert.Equal(0, onStartingCallCount1);
            Assert.Equal(2, onStartingCallCount2);
            Assert.Equal(2, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingInOnCompletedIsLoggedAndClosesConnection(TestServiceContext testContext)
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

                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }

            // All OnCompleted callbacks should be called even if they throw.
            Assert.Equal(2, testLogger.ApplicationErrorsLogged);
            Assert.True(onCompletedCalled1);
            Assert.True(onCompletedCalled2);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task RequestsCanBeAbortedMidRead(TestServiceContext testContext)
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
                using (var connection = server.CreateConnection())
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
                        $"Date: {testContext.DateHeaderValue}",
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

        [MemberData(nameof(ConnectionFilterData))]
        public async Task FailedWritesResultInAbortedRequest(TestServiceContext testContext)
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
                using (var connection = server.CreateConnection())
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

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task NoErrorsLoggedWhenServerEndsConnectionBeforeClient(TestServiceContext testContext)
        {
            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Content-Length"] = new[] { "11" };
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }

            Assert.Equal(0, testLogger.TotalErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task NoResponseSentWhenConnectionIsClosedByServerBeforeClientFinishesSendingRequest(TestServiceContext testContext)
        {
            using (var server = new TestServer(httpContext =>
            {
                httpContext.Abort();
                return TaskCache.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 1",
                        "",
                        "");
                    await connection.ReceiveForcedEnd();
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task RequestHeadersAreResetOnEachRequest(TestServiceContext testContext)
        {
            IHeaderDictionary originalRequestHeaders = null;
            var firstRequest = true;

            using (var server = new TestServer(httpContext =>
            {
                var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();

                if (firstRequest)
                {
                    originalRequestHeaders = requestFeature.Headers;
                    requestFeature.Headers = new FrameRequestHeaders();
                    firstRequest = false;
                }
                else
                {
                    Assert.Same(originalRequestHeaders, requestFeature.Headers);
                }

                return TaskCache.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task ResponseHeadersAreResetOnEachRequest(TestServiceContext testContext)
        {
            IHeaderDictionary originalResponseHeaders = null;
            var firstRequest = true;

            using (var server = new TestServer(httpContext =>
            {
                var responseFeature = httpContext.Features.Get<IHttpResponseFeature>();

                if (firstRequest)
                {
                    originalResponseHeaders = responseFeature.Headers;
                    responseFeature.Headers = new FrameResponseHeaders();
                    firstRequest = false;
                }
                else
                {
                    Assert.Same(originalResponseHeaders, responseFeature.Headers);
                }

                return TaskCache.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData("/%%2000", "/% 00")]
        [InlineData("/%25%30%30", "/%00")]
        public async Task PathEscapeTests(string inputPath, string expectedPath)
        {
            using (var server = new TestServer(async httpContext =>
            {
                var path = httpContext.Request.Path.Value;
                httpContext.Response.Headers["Content-Length"] = new[] { path.Length.ToString() };
                await httpContext.Response.WriteAsync(path);
            }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        $"GET {inputPath} HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Content-Length: {expectedPath.Length.ToString()}",
                        "",
                        $"{expectedPath}");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task OnStartingCallbacksAreCalledInLastInFirstOutOrder(TestServiceContext testContext)
        {
            const string response = "hello, world";

            var callOrder = new Stack<int>();

            using (var server = new TestServer(async context =>
            {
                context.Response.OnStarting(_ =>
                {
                    callOrder.Push(1);
                    return TaskCache.CompletedTask;
                }, null);
                context.Response.OnStarting(_ =>
                {
                    callOrder.Push(2);
                    return TaskCache.CompletedTask;
                }, null);

                context.Response.ContentLength = response.Length;
                await context.Response.WriteAsync(response);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        $"Content-Length: {response.Length}",
                        "",
                        "hello, world");


                }
            }

            Assert.Equal(1, callOrder.Pop());
            Assert.Equal(2, callOrder.Pop());
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task OnCompletedCallbacksAreCalledInLastInFirstOutOrder(TestServiceContext testContext)
        {
            const string response = "hello, world";

            var callOrder = new Stack<int>();

            using (var server = new TestServer(async context =>
            {
                context.Response.OnCompleted(_ =>
                {
                    callOrder.Push(1);
                    return TaskCache.CompletedTask;
                }, null);
                context.Response.OnCompleted(_ =>
                {
                    callOrder.Push(2);
                    return TaskCache.CompletedTask;
                }, null);

                context.Response.ContentLength = response.Length;
                await context.Response.WriteAsync(response);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        $"Content-Length: {response.Length}",
                        "",
                        "hello, world");
                }
            }

            Assert.Equal(1, callOrder.Pop());
            Assert.Equal(2, callOrder.Pop());
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task UpgradeRequestIsNotKeptAliveOrChunked(TestServiceContext testContext)
        {
            const string message = "Hello World";

            using (var server = new TestServer(async context =>
            {
                var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
                var duplexStream = await upgradeFeature.UpgradeAsync();

                var buffer = new byte[message.Length];
                var read = 0;
                while (read < message.Length)
                {
                    read += await duplexStream.ReadAsync(buffer, read, buffer.Length - read).TimeoutAfter(TimeSpan.FromSeconds(10));
                }

                await duplexStream.WriteAsync(buffer, 0, read);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Connection: Upgrade",
                        "",
                        message);
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        message);
                }
            }
        }
    }
}
