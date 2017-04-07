// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class RequestTests
    {
        private const int _connectionStartedEventId = 1;
        private const int _connectionKeepAliveEventId = 9;
        private const int _connectionResetEventId = 19;
        private const int _semaphoreWaitTimeout = 2500;

        public static TheoryData<ListenOptions> ConnectionAdapterData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            }
        };

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
                                    // Do not use Assert.Equal here, it is to slow for this hot path
                                    Assert.True((byte)((total + i) % 256) == receivedBytes[i], "Data received is incorrect");
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

                var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
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
            var connectionReset = new SemaphoreSlim(0);

            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                .Setup(logger => logger.Log(LogLevel.Debug, _connectionStartedEventId, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()))
                .Callback(() =>
                {
                    connectionStarted.Release();
                });
            mockLogger
                .Setup(logger => logger.Log(LogLevel.Debug, _connectionResetEventId, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()))
                .Callback(() =>
                {
                    connectionReset.Release();
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"))
                .Returns(mockLogger.Object);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsNotIn(new[] { "Microsoft.AspNetCore.Server.Kestrel" })))
                .Returns(Mock.Of<ILogger>());

            var builder = new WebHostBuilder()
                .UseLoggerFactory(mockLoggerFactory.Object)
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(context => TaskCache.CompletedTask));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));

                    // Wait until connection is established
                    await connectionStarted.WaitAsync(TimeSpan.FromSeconds(10));

                    // Force a reset
                    socket.LingerState = new LingerOption(true, 0);
                }

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                await connectionReset.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public async Task ConnectionResetBetweenRequestsIsLoggedAsDebug()
        {
            var requestDone = new SemaphoreSlim(0);
            var connectionReset = new SemaphoreSlim(0);

            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                .Setup(logger => logger.Log(LogLevel.Debug, _connectionKeepAliveEventId, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()))
                .Callback(() =>
                {
                    requestDone.Release();
                });
            mockLogger
                .Setup(logger => logger.Log(LogLevel.Debug, _connectionResetEventId, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()))
                .Callback(() =>
                {
                    connectionReset.Release();
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"))
                .Returns(mockLogger.Object);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsNotIn(new[] { "Microsoft.AspNetCore.Server.Kestrel" })))
                .Returns(Mock.Of<ILogger>());


            var builder = new WebHostBuilder()
                .UseLoggerFactory(mockLoggerFactory.Object)
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(context => TaskCache.CompletedTask));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n"));

                    // Wait until request is done being processed
                    await requestDone.WaitAsync(TimeSpan.FromSeconds(10));

                    // Force a reset
                    socket.LingerState = new LingerOption(true, 0);
                }

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                await connectionReset.WaitAsync(TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public async Task ConnectionResetMidRequestIsLoggedAsDebug()
        {
            var connectionReset = new SemaphoreSlim(0);

            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                 .Setup(logger => logger.Log(LogLevel.Debug, _connectionResetEventId, It.IsAny<object>(), null, It.IsAny<Func<object, Exception, string>>()))
                 .Callback(() =>
                 {
                     connectionReset.Release();
                 });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"))
                .Returns(mockLogger.Object);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsNotIn(new[] { "Microsoft.AspNetCore.Server.Kestrel" })))
                .Returns(Mock.Of<ILogger>());

            var requestStarted = new SemaphoreSlim(0);

            var builder = new WebHostBuilder()
                .UseLoggerFactory(mockLoggerFactory.Object)
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:0")
                .Configure(app => app.Run(async context =>
                {
                    requestStarted.Release();
                    await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                }));

            using (var host = builder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                    socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n"));

                    // Wait until connection is established
                    await requestStarted.WaitAsync(TimeSpan.FromSeconds(10));

                    // Force a reset
                    socket.LingerState = new LingerOption(true, 0);
                }

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                await connectionReset.WaitAsync(TimeSpan.FromSeconds(10));
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

        [Theory]
        [InlineData("http://localhost/abs/path", "/abs/path", null)]
        [InlineData("https://localhost/abs/path", "/abs/path", null)] // handles mismatch scheme
        [InlineData("https://localhost:22/abs/path", "/abs/path", null)] // handles mismatched ports
        [InlineData("https://differenthost/abs/path", "/abs/path", null)] // handles mismatched hostname
        [InlineData("http://localhost/", "/", null)]
        [InlineData("http://root@contoso.com/path", "/path", null)]
        [InlineData("http://root:password@contoso.com/path", "/path", null)]
        [InlineData("https://localhost/", "/", null)]
        [InlineData("http://localhost", "/", null)]
        [InlineData("http://127.0.0.1/", "/", null)]
        [InlineData("http://[::1]/", "/", null)]
        [InlineData("http://[::1]:8080/", "/", null)]
        [InlineData("http://localhost?q=123&w=xyz", "/", "123")]
        [InlineData("http://localhost/?q=123&w=xyz", "/", "123")]
        [InlineData("http://localhost/path?q=123&w=xyz", "/path", "123")]
        [InlineData("http://localhost/path%20with%20space?q=abc%20123", "/path with space", "abc 123")]
        public async Task CanHandleRequestsWithUrlInAbsoluteForm(string requestUrl, string expectedPath, string queryValue)
        {
            var pathTcs = new TaskCompletionSource<PathString>();
            var rawTargetTcs = new TaskCompletionSource<string>();
            var hostTcs = new TaskCompletionSource<HostString>();
            var queryTcs = new TaskCompletionSource<IQueryCollection>();

            using (var server = new TestServer(async context =>
                 {
                     pathTcs.TrySetResult(context.Request.Path);
                     hostTcs.TrySetResult(context.Request.Host);
                     queryTcs.TrySetResult(context.Request.Query);
                     rawTargetTcs.TrySetResult(context.Features.Get<IHttpRequestFeature>().RawTarget);
                     await context.Response.WriteAsync("Done");
                 }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        $"GET {requestUrl} HTTP/1.1",
                        "Content-Length: 0",
                        "Host: localhost",
                        "",
                        "");

                    await connection.Receive($"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "4",
                        "Done")
                        .TimeoutAfter(TimeSpan.FromSeconds(10));

                    await Task.WhenAll(pathTcs.Task, rawTargetTcs.Task, hostTcs.Task, queryTcs.Task).TimeoutAfter(TimeSpan.FromSeconds(30));
                    Assert.Equal(new PathString(expectedPath), pathTcs.Task.Result);
                    Assert.Equal(requestUrl, rawTargetTcs.Task.Result);
                    Assert.Equal("localhost", hostTcs.Task.Result.ToString());
                    if (queryValue == null)
                    {
                        Assert.False(queryTcs.Task.Result.ContainsKey("q"));
                    }
                    else
                    {
                        Assert.Equal(queryValue, queryTcs.Task.Result["q"]);
                    }
                }
            }
        }

        [Fact]
        public async Task AppCanSetTraceIdentifier()
        {
            const string knownId = "xyz123";
            using (var server = new TestServer(async context =>
            {
                context.TraceIdentifier = knownId;
                await context.Response.WriteAsync(context.TraceIdentifier);
            }))
            {
                var requestId = await HttpClientSlim.GetStringAsync($"http://{server.EndPoint}")
                    .TimeoutAfter(TimeSpan.FromSeconds(10));
                Assert.Equal(knownId, requestId);
            }
        }

        [Fact]
        public async Task TraceIdentifierIsUnique()
        {
            const int IdentifierLength = 13;
            const int iterations = 10;

            using (var server = new TestServer(async context =>
            {
                Assert.Equal(IdentifierLength, Encoding.ASCII.GetByteCount(context.TraceIdentifier));
                context.Response.ContentLength = IdentifierLength;
                await context.Response.WriteAsync(context.TraceIdentifier);
            }))
            {
                var usedIds = new ConcurrentBag<string>();
                var uri = $"http://{server.EndPoint}";

                // requests on separate connections in parallel
                Parallel.For(0, iterations, async i =>
                {
                    var id = await HttpClientSlim.GetStringAsync(uri);
                    Assert.DoesNotContain(id, usedIds.ToArray());
                    usedIds.Add(id);
                });

                // requests on same connection
                using (var connection = server.CreateConnection())
                {
                    var buffer = new char[IdentifierLength];
                    for (var i = 0; i < iterations; i++)
                    {
                        await connection.Send("GET / HTTP/1.1",
                            "",
                            "");

                        await connection.Receive($"HTTP/1.1 200 OK",
                           $"Date: {server.Context.DateHeaderValue}",
                           $"Content-Length: {IdentifierLength}",
                           "",
                           "").TimeoutAfter(TimeSpan.FromSeconds(10));

                        var read = await connection.Reader.ReadAsync(buffer, 0, IdentifierLength);
                        Assert.Equal(IdentifierLength, read);
                        var id = new string(buffer, 0, read);
                        Assert.DoesNotContain(id, usedIds.ToArray());
                        usedIds.Add(id);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Http11KeptAliveByDefault(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext, listenOptions))
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Http10NotKeptAliveByDefault(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoApp, testContext, listenOptions))
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
                        "Content-Length: 0",
                        "",
                        "");
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Http10KeepAlive(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext, listenOptions))
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
                    await connection.ReceiveForcedEnd(
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Http10KeepAliveNotHonoredIfResponseContentLengthNotSet(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoApp, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.0",
                        "Connection: keep-alive",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "\r\n");

                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 7",
                        "",
                        "Goodbye");

                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "Goodbye");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Http10KeepAliveHonoredIfResponseContentLengthSet(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 11",
                        "Connection: keep-alive",
                        "",
                        "Hello World");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");

                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Connection: keep-alive",
                        "Content-Length: 11",
                        "",
                        "Hello Again");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello Again");

                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 7",
                        "",
                        "Goodbye");

                    await connection.ReceiveForcedEnd(
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Expect100ContinueHonored(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext, listenOptions))
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
                    await connection.ReceiveForcedEnd(
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroContentLengthAssumedOnNonKeepAliveRequestsWithoutContentLengthOrTransferEncodingHeader(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(async httpContext =>
            {
                // This will hang if 0 content length is not assumed by the server
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).TimeoutAfter(TimeSpan.FromSeconds(10)));
            }, testContext, listenOptions))
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionClosesWhenFinReceivedBeforeRequestCompletes(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            using (var server = new TestServer(TestApp.EchoAppChunked, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1");
                    connection.Shutdown(SocketShutdown.Send);
                    await connection.ReceiveForcedEnd();
                }

                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Content-Length: 7");
                    connection.Shutdown(SocketShutdown.Send);
                    await connection.ReceiveForcedEnd();
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task RequestsCanBeAbortedMidRead(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

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
            }, testContext, listenOptions))
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

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task RequestHeadersAreResetOnEachRequest(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

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
            }, testContext, listenOptions))
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task UpgradeRequestIsNotKeptAliveOrChunked(ListenOptions listenOptions)
        {
            const string message = "Hello World";

            var testContext = new TestServiceContext();

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
            }, testContext, listenOptions))
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

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task HeadersAndStreamsAreReusedAcrossRequests(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();
            var streamCount = 0;
            var requestHeadersCount = 0;
            var responseHeadersCount = 0;
            var loopCount = 20;
            Stream lastStream = null;
            IHeaderDictionary lastRequestHeaders = null;
            IHeaderDictionary lastResponseHeaders = null;

            using (var server = new TestServer(async context =>
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
            }, testContext))
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
    }
}
