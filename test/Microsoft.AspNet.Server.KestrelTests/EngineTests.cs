// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Filter;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
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
                        new TestServiceContext
                        {
                            ConnectionFilter = new NoOpConnectionFilter()
                        }
                    }
                };
            }
        }

        private async Task App(IFeatureCollection frame)
        {
            var request = frame.Get<IHttpRequestFeature>();
            var response = frame.Get<IHttpResponseFeature>();
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

        private async Task AppChunked(IFeatureCollection frame)
        {
            var request = frame.Get<IHttpRequestFeature>();
            var response = frame.Get<IHttpResponseFeature>();
            var data = new MemoryStream();
            await request.Body.CopyToAsync(data);
            var bytes = data.ToArray();

            response.Headers.Clear();
            response.Headers["Content-Length"] = bytes.Length.ToString();
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        private Task EmptyApp(IFeatureCollection frame)
        {
            frame.Get<IHttpResponseFeature>().Headers.Clear();
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
        public void ListenerCanCreateAndDispose(ServiceContext testContext)
        {
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            var address = ServerAddress.FromUrl("http://localhost:54321/");
            var started = engine.CreateServer(address, App);
            started.Dispose();
            engine.Dispose();
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public void ConnectionCanReadAndWrite(ServiceContext testContext)
        {
            var engine = new KestrelEngine(testContext);
            engine.Start(1);
            var address = ServerAddress.FromUrl("http://localhost:54321/");
            var started = engine.CreateServer(address, App);

            Console.WriteLine("Started");
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 54321));
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
                using (var connection = new TestConnection())
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
                using (var connection = new TestConnection())
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
                        "Content-Length: 7",
                        "Connection: close",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionFilterData))]
        public async Task Http10ContentLength(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                using (var connection = new TestConnection())
                {
                    await connection.Send(
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
        public async Task Http10TransferEncoding(ServiceContext testContext)
        {
            using (var server = new TestServer(App, testContext))
            {
                using (var connection = new TestConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Transfer-Encoding: chunked",
                        "",
                        "5", "Hello", "6", " World", "0\r\n");
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
                using (var connection = new TestConnection())
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
                        "Content-Length: 0",
                        "Connection: keep-alive",
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
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Content-Length: 0",
                        "Connection: keep-alive",
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
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 11",
                        "",
                        "Hello WorldPOST / HTTP/1.0",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Content-Length: 11",
                        "Connection: keep-alive",
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
        public async Task Http10KeepAliveTransferEncoding(ServiceContext testContext)
        {
            using (var server = new TestServer(AppChunked, testContext))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.0",
                        "Transfer-Encoding: chunked",
                        "Connection: keep-alive",
                        "",
                        "5", "Hello", "6", " World", "0",
                        "POST / HTTP/1.0",
                        "",
                        "Goodbye");
                    await connection.Receive(
                        "HTTP/1.0 200 OK",
                        "Content-Length: 11",
                        "Connection: keep-alive",
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
                using (var connection = new TestConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Expect: 100-continue",
                        "Content-Length: 11",
                        "Connection: close",
                        "\r\n");
                    await connection.Receive("HTTP/1.1 100 Continue", "\r\n");
                    await connection.SendEnd("Hello World");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "Connection: close",
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
                var socket = new Socket(SocketType.Stream, ProtocolType.IP);
                socket.Connect(IPAddress.Loopback, 54321);
                await Task.Delay(200);
                socket.Dispose();

                await Task.Delay(200);
                using (var connection = new TestConnection())
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
                using (var connection = new TestConnection())
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
                        "Content-Length: 0",
                        "Connection: keep-alive",
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
                using (var connection = new TestConnection())
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

                using (var connection = new TestConnection())
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
                using (var connection = new TestConnection())
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
            using (var server = new TestServer(async frame =>
            {
                var request = frame.Get<IHttpRequestFeature>();
                var response = frame.Get<IHttpResponseFeature>();
                response.Headers.Clear();

                using (var reader = new StreamReader(request.Body, Encoding.ASCII))
                {
                    var statusString = await reader.ReadLineAsync();
                    response.StatusCode = int.Parse(statusString);
                }
            }, testContext))
            {
                using (var connection = new TestConnection())
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

            using (var server = new TestServer(frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
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
                using (var connection = new TestConnection())
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
                    await connection.ReceiveStartsWith("Date:");
                    await connection.ReceiveEnd(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "Connection: close",
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

            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
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
                using (var connection = new TestConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
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

            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
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
                using (var connection = new TestConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
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
                using (var connection = new TestConnection())
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
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "GET /");
                    await connection.ReceiveEnd();
                }

                using (var connection = new TestConnection())
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

                using (var connection = new TestConnection())
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

            using (var server = new TestServer(async frame =>
            {
                var onStartingException = new Exception();

                var response = frame.Get<IHttpResponseFeature>();
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
                using (var connection = new TestConnection())
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
                    await connection.ReceiveStartsWith("Date:");
                    await connection.ReceiveEnd(
                        "Content-Length: 0",
                        "Server: Kestrel",
                        "Connection: close",
                        "",
                        "");

                    Assert.Equal(2, onStartingCallCount1);
                    // The second OnStarting callback should not be called since the first failed.
                    Assert.Equal(0, onStartingCallCount2);
                    Assert.Equal(2, testLogger.ApplicationErrorsLogged);
                }
            }
        }

        [MemberData(nameof(ConnectionFilterData))]
        public async Task ThrowingInOnCompletedIsLoggedAndClosesConnection(ServiceContext testContext)
        {
            var onCompletedCalled1 = false;
            var onCompletedCalled2 = false;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
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
                using (var connection = new TestConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.ReceiveEnd(
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
        public async Task RequestBodyIsConsumedAutomaticallyIfAppDoesntConsumeItFully(ServiceContext testContext)
        {
            using (var server = new TestServer(async frame =>
            {
                var response = frame.Get<IHttpResponseFeature>();
                var request = frame.Get<IHttpRequestFeature>();

                Assert.Equal("POST", request.Method);

                response.Headers.Clear();
                response.Headers["Content-Length"] = new[] { "11" };

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext))
            {
                using (var connection = new TestConnection())
                {
                    await connection.SendEnd(
                        "POST / HTTP/1.1",
                        "Content-Length: 5",
                        "",
                        "HelloPOST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "",
                        "C", "HelloChunked", "0",
                        "POST / HTTP/1.1",
                        "Content-Length: 7",
                        "",
                        "Goodbye");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello WorldHTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello WorldHTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }
        }

        private class TestApplicationErrorLogger : ILogger
        {
            public int ApplicationErrorsLogged { get; set; }

            public IDisposable BeginScopeImpl(object state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                throw new NotImplementedException();
            }

            public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                // Application errors are logged using 13 as the eventId.
                if (eventId == 13)
                {
                    ApplicationErrorsLogged++;
                }
            }
        }
    }
}
