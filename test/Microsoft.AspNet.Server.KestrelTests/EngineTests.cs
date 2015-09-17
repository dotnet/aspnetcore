// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Xunit;
using System.Linq;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for EngineTests
    /// </summary>
    public class EngineTests
    {
        private async Task App(Frame frame)
        {
            frame.ResponseHeaders.Clear();
            while (true)
            {
                var buffer = new byte[8192];
                var count = await frame.RequestBody.ReadAsync(buffer, 0, buffer.Length);
                if (count == 0)
                {
                    break;
                }
                await frame.ResponseBody.WriteAsync(buffer, 0, count);
            }
        }

        ILibraryManager LibraryManager
        {
            get
            {
                try
                {
                    var locator = CallContextServiceLocator.Locator;
                    if (locator == null)
                    {
                        return null;
                    }
                    var services = locator.ServiceProvider;
                    if (services == null)
                    {
                        return null;
                    }
                    return (ILibraryManager)services.GetService(typeof(ILibraryManager));
                }
                catch (NullReferenceException)
                { return null; }
            }
        }

        private async Task AppChunked(Frame frame)
        {
            foreach (var h in frame.RequestHeaders)
            {
                Console.WriteLine($"{h.Key}: {h.Value}");
            }
            Console.WriteLine($"");

            frame.ResponseHeaders.Clear();
            var data = new MemoryStream();
            while(true)
            {
                await frame.RequestBody.CopyToAsync(data);
            }
            var bytes = data.ToArray();
            frame.ResponseHeaders["Content-Length"] = new[] { bytes.Length.ToString() };
            await frame.ResponseBody.WriteAsync(bytes, 0, bytes.Length);
        }

        private Task EmptyApp(Frame frame)
        {
            frame.ResponseHeaders.Clear();
            return Task.FromResult<object>(null);
        }

        [Fact]
        public void EngineCanStartAndStop()
        {
            var engine = new KestrelEngine(LibraryManager, new ShutdownNotImplemented(), new TestLogger());
            engine.Start(1);
            engine.Dispose();
        }

        [Fact]
        public void ListenerCanCreateAndDispose()
        {
            var engine = new KestrelEngine(LibraryManager, new ShutdownNotImplemented(), new TestLogger());
            engine.Start(1);
            var started = engine.CreateServer("http", "localhost", 54321, App);
            started.Dispose();
            engine.Dispose();
        }


        [Fact]
        public void ConnectionCanReadAndWrite()
        {
            var engine = new KestrelEngine(LibraryManager, new ShutdownNotImplemented(), new TestLogger());
            engine.Start(1);
            var started = engine.CreateServer("http", "localhost", 54321, App);

            Console.WriteLine("Started");
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Loopback, 54321));
            socket.Send(Encoding.ASCII.GetBytes("POST / HTTP/1.0\r\n\r\nHello World"));
            socket.Shutdown(SocketShutdown.Send);
            var buffer = new byte[8192];
            for (;;)
            {
                var length = socket.Receive(buffer);
                if (length == 0) { break; }
                var text = Encoding.ASCII.GetString(buffer, 0, length);
            }
            started.Dispose();
            engine.Dispose();
        }

        [Fact]
        public async Task Http10()
        {
            using (var server = new TestServer(App))
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

        [Fact]
        public async Task Http11()
        {
            using (var server = new TestServer(AppChunked))
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


        [Fact]
        public async Task Http10ContentLength()
        {
            using (var server = new TestServer(App))
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

        [Fact]
        public async Task Http10TransferEncoding()
        {
            using (var server = new TestServer(App))
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


        [Fact]
        public async Task Http10KeepAlive()
        {
            using (var server = new TestServer(AppChunked))
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

        [Fact]
        public async Task Http10KeepAliveNotUsedIfResponseContentLengthNotSet()
        {
            using (var server = new TestServer(App))
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

        [Fact]
        public async Task Http10KeepAliveContentLength()
        {
            using (var server = new TestServer(AppChunked))
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

        [Fact]
        public async Task Http10KeepAliveTransferEncoding()
        {
            using (var server = new TestServer(AppChunked))
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

        [Fact]
        public async Task Expect100ContinueForBody()
        {
            using (var server = new TestServer(AppChunked))
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


        [Fact]
        public async Task DisconnectingClient()
        {
            using (var server = new TestServer(App))
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.IP);
                socket.Connect(IPAddress.Loopback, 54321);
                await Task.Delay(200);
                socket.Disconnect(false);
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

        [Fact]
        public async Task ZeroContentLengthSetAutomaticallyAfterNoWrites()
        {
            using (var server = new TestServer(EmptyApp))
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

        [Fact]
        public async Task ZeroContentLengthNotSetAutomaticallyForNonKeepAliveRequests()
        {
            using (var server = new TestServer(EmptyApp))
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

        [Fact]
        public async Task ZeroContentLengthNotSetAutomaticallyForHeadRequests()
        {
            using (var server = new TestServer(EmptyApp))
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

        [Fact]
        public async Task ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes()
        {
            using (var server = new TestServer(async frame =>
            {
                frame.ResponseHeaders.Clear();

                using (var reader = new StreamReader(frame.RequestBody, Encoding.ASCII))
                {
                    var statusString = await reader.ReadLineAsync();
                    frame.StatusCode = int.Parse(statusString);
                }
            }))
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

        [Fact]
        public async Task ThrowingResultsIn500Response()
        {
            bool onStartingCalled = false;

            using (var server = new TestServer(frame =>
            {
                frame.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                // Anything added to the ResponseHeaders dictionary is ignored
                frame.ResponseHeaders.Clear();
                frame.ResponseHeaders["Content-Length"] = new[] { "11" };
                throw new Exception();
            }))
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
                }
            }
        }

        [Fact]
        public async Task ThrowingAfterWritingKillsConnection()
        {
            bool onStartingCalled = false;

            using (var server = new TestServer(async frame =>
            {
                frame.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                frame.ResponseHeaders.Clear();
                frame.ResponseHeaders["Content-Length"] = new[] { "11" };
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
                throw new Exception();
            }))
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
                }
            }
        }

        [Fact]
        public async Task ThrowingAfterPartialWriteKillsConnection()
        {
            bool onStartingCalled = false;

            using (var server = new TestServer(async frame =>
            {
                frame.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                frame.ResponseHeaders.Clear();
                frame.ResponseHeaders["Content-Length"] = new[] { "11" };
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("Hello"), 0, 5);
                throw new Exception();
            }))
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
                }
            }
        }

        [Fact]
        public async Task ConnectionClosesWhenFinReceived()
        {
            using (var server = new TestServer(AppChunked))
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

        [Fact]
        public async Task ConnectionClosesWhenFinReceivedBeforeRequestCompletes()
        {
            using (var server = new TestServer(AppChunked))
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

        [Fact]
        public async Task ThrowingInOnStartingResultsIn500Response()
        {
            using (var server = new TestServer(frame =>
            {
                frame.OnStarting(_ =>
                {
                    throw new Exception();
                }, null);

                frame.ResponseHeaders.Clear();
                frame.ResponseHeaders["Content-Length"] = new[] { "11" };

                // If we write to the response stream, we will not get a 500.

                return Task.FromResult<object>(null);
            }))
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
                }
            }
        }

        [Fact]
        public async Task ThrowingInOnStartingResultsInFailedWrites()
        {
            using (var server = new TestServer(async frame =>
            {
                var onStartingException = new Exception();

                frame.OnStarting(_ =>
                {
                    throw onStartingException;
                }, null);

                frame.ResponseHeaders.Clear();
                frame.ResponseHeaders["Content-Length"] = new[] { "11" };

                var writeException = await Assert.ThrowsAsync<Exception>(async () =>
                    await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11));

                Assert.Same(onStartingException, writeException);

                // The second write should succeed since the OnStarting callback will not be called again
                await frame.ResponseBody.WriteAsync(Encoding.ASCII.GetBytes("Exception!!"), 0, 11);
            }))
            {
                using (var connection = new TestConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Content-Length: 11",
                        "",
                        "Exception!!"); ;
                }
            }
        }
    }
}
