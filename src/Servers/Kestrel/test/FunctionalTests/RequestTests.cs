// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using System.Diagnostics.Metrics;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
#endif

public class RequestTests : LoggedTest
{
    private const int _connectionStartedEventId = 1;
    private const int _connectionReadFinEventId = 6;
    private const int _connectionResetEventId = 19;
    private static readonly int _semaphoreWaitTimeout = Debugger.IsAttached ? 10000 : 2500;

    public static Dictionary<string, Func<ListenOptions>> ConnectionMiddlewareData { get; } = new Dictionary<string, Func<ListenOptions>>
    {
        { "Loopback", () => new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)) },
        { "PassThrough", () => new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)).UsePassThrough() }
    };

    public static TheoryData<string> ConnectionMiddlewareDataName => new TheoryData<string>
    {
        "Loopback",
        "PassThrough"
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

        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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

                            await context.Response.WriteAsync($"bytesRead: {total}");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

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
        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseUrls("http://127.0.0.1:0")
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

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
        var ensureConcurrentRequestTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async context =>
        {
            if (Interlocked.Increment(ref requestNumber) == 1)
            {
                await ensureConcurrentRequestTcs.Task.DefaultTimeout();
            }
            else
            {
                ensureConcurrentRequestTcs.SetResult();
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
                await connection2.Receive($"HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ConnectionClosedPriorToRequestIsLoggedAsDebug()
    {
        var connectionStarted = new SemaphoreSlim(0);
        var connectionReadFin = new SemaphoreSlim(0);
        var loggedHigherThanDebug = false;

        TestSink.MessageLogged += context =>
        {
            if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel" &&
                context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Connections" &&
                context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
            {
                return;
            }

            if (context.EventId.Id == _connectionStartedEventId)
            {
                connectionStarted.Release();
            }
            else if (context.EventId.Id == _connectionReadFinEventId)
            {
                connectionReadFin.Release();
            }

            if (context.LogLevel > LogLevel.Debug)
            {
                loggedHigherThanDebug = true;
            }
        };

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                // Wait until connection is established
                Assert.True(await connectionStarted.WaitAsync(TestConstants.DefaultTimeout));

                connection.ShutdownSend();

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                Assert.True(await connectionReadFin.WaitAsync(TestConstants.DefaultTimeout));
                await connection.ReceiveEnd();
            }
        }

        Assert.False(loggedHigherThanDebug);
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
                context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Connections" &&
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

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
        }

        Assert.False(loggedHigherThanDebug);
    }

    [Fact]
    public async Task ConnectionClosedBetweenRequestsIsLoggedAsDebug()
    {
        var connectionReadFin = new SemaphoreSlim(0);
        var loggedHigherThanDebug = false;

        TestSink.MessageLogged += context =>
        {
            if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel" &&
                context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
            {
                return;
            }

            if (context.LogLevel > LogLevel.Debug)
            {
                loggedHigherThanDebug = true;
            }

            if (context.EventId.Id == _connectionReadFinEventId)
            {
                connectionReadFin.Release();
            }
        };

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                connection.ShutdownSend();

                // If the reset is correctly logged as Debug, the wait below should complete shortly.
                // This check MUST come before disposing the server, otherwise there's a race where the RST
                // is still in flight when the connection is aborted, leading to the reset never being received
                // and therefore not logged.
                Assert.True(await connectionReadFin.WaitAsync(TestConstants.DefaultTimeout));

                await connection.ReceiveEnd();
            }
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

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
        }

        Assert.False(loggedHigherThanDebug);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConnectionClosedOrResetMidRequestIsLoggedAsDebug(bool close)
    {
        var requestStarted = new SemaphoreSlim(0);
        var connectionReadFin = new SemaphoreSlim(0);
        var connectionReset = new SemaphoreSlim(0);
        var connectionClosing = new SemaphoreSlim(0);
        var loggedHigherThanDebug = false;

        TestSink.MessageLogged += context =>
        {
            if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel" &&
                context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
            {
                return;
            }

            if (context.LogLevel > LogLevel.Debug)
            {
                loggedHigherThanDebug = true;
            }

            if (context.EventId.Id == _connectionReadFinEventId)
            {
                connectionReadFin.Release();
            }

            if (context.EventId.Id == _connectionResetEventId)
            {
                connectionReset.Release();
            }
        };

        await using (var server = new TestServer(async context =>
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

                if (close)
                {
                    connection.ShutdownSend();
                    Assert.True(await connectionReadFin.WaitAsync(TestConstants.DefaultTimeout), "Connection close event should have been logged");
                }
                else
                {
                    connection.Reset();

                    // If the reset is correctly logged as Debug, the wait below should complete shortly.
                    // This check MUST come before disposing the server, otherwise there's a race where the RST
                    // is still in flight when the connection is aborted, leading to the reset never being received
                    // and therefore not logged.
                    Assert.True(await connectionReset.WaitAsync(TestConstants.DefaultTimeout), "Connection reset event should have been logged");
                }
                connectionClosing.Release();
            }
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

        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
            })
            .ConfigureServices(AddTestLogging);

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
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var appStarted = new SemaphoreSlim(0);
        var requestAborted = new SemaphoreSlim(0);
        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseUrls("http://127.0.0.1:0")
                    .Configure(app => app.Run(async context =>
                    {
                        appStarted.Release();

                        var token = context.RequestAborted;
                        token.Register(() => requestAborted.Release(2));
                        await requestAborted.WaitAsync().DefaultTimeout();
                    }));
            })
            .ConfigureServices(AddTestLogging)
            .ConfigureServices(s => s.AddSingleton<IMeterFactory>(testMeterFactory));

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

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Fact]
    public async Task RequestAbortedTokenUnchangedOnAbort()
    {
        var appDoneTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationToken? beforeAbort = null;
        CancellationToken? afterAbort = null;

        await using (var server = new TestServer(async context =>
        {
            var abortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            context.RequestAborted.Register(() =>
            {
                abortedTcs.SetResult();
            });

            beforeAbort = context.RequestAborted;

            context.Abort();

            // Abort doesn't happen inline. Need to wait for it to complete before reading token again.
            await abortedTcs.Task;

            afterAbort = context.RequestAborted;

            appDoneTcs.SetResult();
        }, new TestServiceContext(LoggerFactory)))
        {
            using (var connection1 = server.CreateConnection())
            {
                await connection1.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");

                await appDoneTcs.Task.DefaultTimeout();
            }
        }

        Assert.NotNull(beforeAbort);
        Assert.NotNull(afterAbort);
        Assert.Equal(beforeAbort.Value, afterAbort.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AbortingTheConnection(bool fin)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseSockets(options =>
                    {
                        options.FinOnError = fin;
                    })
                    .UseKestrel(o =>
                    {
                        o.FinOnError = fin;
                    })
                    .UseUrls("http://127.0.0.1:0")
                    .Configure(app => app.Run(context =>
                    {
                        context.Abort();
                        return Task.CompletedTask;
                    }));
            })
            .ConfigureServices(AddTestLogging)
            .ConfigureServices(s => s.AddSingleton<IMeterFactory>(testMeterFactory));

        using (var host = builder.Build())
        {
            await host.StartAsync();

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));
                socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n"));
                if (fin)
                {
                    int result = socket.Receive(new byte[32]);
                    Assert.Equal(0, result);
                }
                else
                {
                    Assert.Throws<SocketException>(() => socket.Receive(new byte[32]));
                }
            }

            await host.StopAsync();
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.AbortedByApp, m.Tags));
    }

    [Theory]
    [MemberData(nameof(ConnectionMiddlewareDataName))]
    public async Task ConnectionClosedTokenFiresOnClientFIN(string listenOptionsName)
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var appStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(context =>
        {
            appStartedTcs.SetResult();

            var connectionLifetimeFeature = context.Features.Get<IConnectionLifetimeFeature>();
            connectionLifetimeFeature.ConnectionClosed.Register(() => connectionClosedTcs.SetResult());

            return Task.CompletedTask;
        }, testContext, ConnectionMiddlewareData[listenOptionsName]()))
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
        }
    }

    [Theory]
    [MemberData(nameof(ConnectionMiddlewareDataName))]
    public async Task ConnectionClosedTokenFiresOnServerFIN(string listenOptionsName)
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(context =>
        {
            var connectionLifetimeFeature = context.Features.Get<IConnectionLifetimeFeature>();
            connectionLifetimeFeature.ConnectionClosed.Register(() => connectionClosedTcs.SetResult());

            return Task.CompletedTask;
        }, testContext, ConnectionMiddlewareData[listenOptionsName]()))
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Theory]
    [MemberData(nameof(ConnectionMiddlewareDataName))]
    public async Task ConnectionClosedTokenFiresOnServerAbort(string listenOptionsName)
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var connectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(context =>
        {
            var connectionLifetimeFeature = context.Features.Get<IConnectionLifetimeFeature>();
            connectionLifetimeFeature.ConnectionClosed.Register(() => connectionClosedTcs.SetResult());

            context.Abort();

            return Task.CompletedTask;
        }, testContext, ConnectionMiddlewareData[listenOptionsName]()))
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
        }
    }

    [Theory]
    [MemberData(nameof(ConnectionMiddlewareDataName))]
    public async Task RequestsCanBeAbortedMidRead(string listenOptionsName)
    {
        // This needs a timeout.
        const int applicationAbortedConnectionId = 34;

        var testContext = new TestServiceContext(LoggerFactory);

        var readTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var registrationTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestId = 0;

        await using (var server = new TestServer(async httpContext =>
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
        }, testContext, ConnectionMiddlewareData[listenOptionsName]()))
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
                    "Content-Length: 5",
                    $"Date: {testContext.DateHeaderValue}",
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
        }

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await readTcs.Task);

        // The cancellation token for only the last request should be triggered.
        var abortedRequestId = await registrationTcs.Task.DefaultTimeout();
        Assert.Equal(2, abortedRequestId);

        Assert.Single(TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Connections" &&
                                                    w.EventId == applicationAbortedConnectionId));
    }

    [Theory]
    [InlineData("Loopback", true)]
    [InlineData("Loopback", false)]
    [InlineData("PassThrough", true)]
    [InlineData("PassThrough", false)]
    public async Task ServerCanAbortConnectionAfterUnobservedClose(string listenOptionsName, bool fin)
    {
        const int connectionPausedEventId = 4;
        const int connectionFinSentEventId = 7;
        const int connectionRstSentEventId = 8;
        const int maxRequestBufferSize = 4096;

        var readCallbackUnwired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientClosedConnection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverFinConnection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var serverRstConnection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appFuncCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {
            if (context.LoggerName != "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")
            {
                return;
            }

            if (context.EventId.Id == connectionPausedEventId)
            {
                readCallbackUnwired.TrySetResult();
            }
            else if (context.EventId == connectionFinSentEventId)
            {
                serverFinConnection.SetResult();
            }
            else if (context.EventId == connectionRstSentEventId)
            {
                serverRstConnection.SetResult();
            }
        };

        var testContext = new TestServiceContext(LoggerFactory)
        {
            ServerOptions =
            {
                FinOnError = fin,
                Limits =
                {
                    MaxRequestBufferSize = maxRequestBufferSize,
                    MaxRequestLineSize = maxRequestBufferSize,
                    MaxRequestHeadersTotalSize = maxRequestBufferSize,
                }
            }
        };

        var scratchBuffer = new byte[maxRequestBufferSize * 8];

        await using (var server = new TestServer(async context =>
        {
            await clientClosedConnection.Task;

            context.Abort();

            if (fin)
            {
                await serverFinConnection.Task.DefaultTimeout();
            }
            else
            {
                await serverRstConnection.Task.DefaultTimeout();
            }

            appFuncCompleted.SetResult();
        }, testContext, listen =>
        {
            if (listenOptionsName == "PassThrough")
            {
                listen.UsePassThrough();
            }
        },
        services =>
        {
            services.Configure<SocketTransportOptions>(options =>
            {
                options.FinOnError = fin;
            });
        }))
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

            clientClosedConnection.SetResult();

            await appFuncCompleted.Task.DefaultTimeout();
        }

        Assert.Single(TestSink.Writes.Where(c => c.EventId.Name == "ConnectionStop"));
    }

    [Theory]
    [MemberData(nameof(ConnectionMiddlewareDataName))]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/27157")]
    public async Task AppCanHandleClientAbortingConnectionMidRequest(string listenOptionsName)
    {
        var readTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var testContext = new TestServiceContext(LoggerFactory);

        var scratchBuffer = new byte[4096];

        await using (var server = new TestServer(async context =>
        {
            appStartedTcs.SetResult();

            try
            {
                await context.Request.Body.CopyToAsync(Stream.Null);
            }
            catch (Exception ex)
            {
                readTcs.SetException(ex);
                throw;
            }

            readTcs.SetException(new Exception("This shouldn't be reached."));

        }, testContext, ConnectionMiddlewareData[listenOptionsName]()))
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
        }

        Assert.Single(TestSink.Writes.Where(c => c.EventId.Name == "ConnectionStop"));
    }

    private async Task TestRemoteIPAddress(string registerAddress, string requestAddress, string expectAddress)
    {
        var builder = TransportSelector.GetHostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
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
            })
            .ConfigureServices(AddTestLogging);

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
        var expectedLength = expectedBytes.Length;
        var responseBuffer = new byte[expectedLength];

        var matchedChars = 0;

        while (matchedChars < expectedLength)
        {
            var count = await stream.ReadAsync(responseBuffer, 0, expectedLength - matchedChars).DefaultTimeout();

            if (count == 0)
            {
                Assert.True(false, "Stream completed without expected substring.");
            }

            for (var i = 0; i < count && matchedChars < expectedLength; i++)
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
