// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Tests;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ResponseTests : TestApplicationErrorLoggerLoggedTest
    {
        public static TheoryData<ListenOptions> ConnectionAdapterData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            }
        };

        [Fact]
        public async Task LargeDownload()
        {
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        var bytes = new byte[1024];
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] = (byte)i;
                        }

                        context.Response.ContentLength = bytes.Length * 1024;

                        for (int i = 0; i < 1024; i++)
                        {
                            await context.Response.BodyWriter.WriteAsync(new Memory<byte>(bytes, 0, bytes.Length));
                        }
                    });
                });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStreamAsync();

                    // Read the full response body
                    var total = 0;
                    var bytes = new byte[1024];
                    var count = await responseBody.ReadAsync(bytes, 0, bytes.Length);
                    while (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            Assert.Equal(total % 256, bytes[i]);
                            total++;
                        }
                        count = await responseBody.ReadAsync(bytes, 0, bytes.Length);
                    }
                }
                await host.StopAsync();
            }
        }

        [Theory, MemberData(nameof(NullHeaderData))]
        public async Task IgnoreNullHeaderValues(string headerName, StringValues headerValue, string expectedValue)
        {
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.Headers.Add(headerName, headerValue);

                        await context.Response.WriteAsync("");
                    });
                });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
                    response.EnsureSuccessStatusCode();

                    var headers = response.Headers;

                    if (expectedValue == null)
                    {
                        Assert.False(headers.Contains(headerName));
                    }
                    else
                    {
                        Assert.True(headers.Contains(headerName));
                        Assert.Equal(headers.GetValues(headerName).Single(), expectedValue);
                    }
                }
                await host.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task WriteAfterConnectionCloseNoops(ListenOptions listenOptions)
        {
            var connectionClosed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestStarted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async httpContext =>
            {
                try
                {
                    requestStarted.SetResult(null);
                    await connectionClosed.Task.DefaultTimeout();
                    httpContext.Response.ContentLength = 12;
                    await httpContext.Response.WriteAsync("hello, world");
                    appCompleted.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    appCompleted.TrySetException(ex);
                }
            }, new TestServiceContext(LoggerFactory), listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    await requestStarted.Task.DefaultTimeout();
                    connection.ShutdownSend();
                    await connection.WaitForConnectionClose();
                }

                connectionClosed.SetResult(null);

                await appCompleted.Task.DefaultTimeout();
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowsOnWriteWithRequestAbortedTokenAfterRequestIsAborted(ListenOptions listenOptions)
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;

            // Ensure string is long enough to disable write-behind buffering
            var largeString = new string('a', maxBytesPreCompleted + 1);

            var writeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestAbortedWh = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestStartWh = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async httpContext =>
            {
                requestStartWh.SetResult(null);

                var response = httpContext.Response;
                var request = httpContext.Request;
                var lifetime = httpContext.Features.Get<IHttpRequestLifetimeFeature>();

                lifetime.RequestAborted.Register(() => requestAbortedWh.SetResult(null));
                await requestAbortedWh.Task.DefaultTimeout();

                try
                {
                    await response.WriteAsync(largeString, cancellationToken: lifetime.RequestAborted);
                }
                catch (Exception ex)
                {
                    writeTcs.SetException(ex);
                    throw;
                }
                finally
                {
                    await requestAbortedWh.Task.DefaultTimeout();
                }

                writeTcs.SetException(new Exception("This shouldn't be reached."));
            }, new TestServiceContext(LoggerFactory), listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 0",
                        "",
                        "");

                    await requestStartWh.Task.DefaultTimeout();
                }

                // Write failed - can throw TaskCanceledException or OperationCanceledException,
                // depending on how far the canceled write goes.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await writeTcs.Task).DefaultTimeout();

                // RequestAborted tripped
                await requestAbortedWh.Task.DefaultTimeout();
                await server.StopAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task WritingToConnectionAfterUnobservedCloseTriggersRequestAbortedToken(ListenOptions listenOptions)
        {
            const int connectionPausedEventId = 4;
            const int maxRequestBufferSize = 4096;

            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var readCallbackUnwired = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientClosedConnection = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                context.RequestAborted.Register(() => requestAborted.SetResult(null));

                await clientClosedConnection.Task;

                try
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        await context.Response.BodyWriter.WriteAsync(new Memory<byte>(scratchBuffer, 0, scratchBuffer.Length), context.RequestAborted);
                        await Task.Delay(10);
                    }
                }
                catch (Exception ex)
                {
                    writeTcs.SetException(ex);
                    throw;
                }
                finally
                {
                    await requestAborted.Task.DefaultTimeout();
                }

                writeTcs.SetException(new Exception("This shouldn't be reached."));
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

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => writeTcs.Task).DefaultTimeout();
                await server.StopAsync();
            }

            mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.Once());
            Assert.True(requestAborted.Task.IsCompleted);
        }

        [Theory(Skip = "https://github.com/aspnet/AspNetCore/issues/7342")]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task AppCanHandleClientAbortingConnectionMidResponse(ListenOptions listenOptions)
        {
            const int connectionResetEventId = 19;
            const int connectionFinEventId = 6;
            const int connectionStopEventId = 2;

            const int responseBodySegmentSize = 65536;
            const int responseBodySegmentCount = 100;

            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var scratchBuffer = new byte[responseBodySegmentSize];

            using (var server = new TestServer(async context =>
            {
                context.RequestAborted.Register(() => requestAborted.SetResult(null));

                for (var i = 0; i < responseBodySegmentCount; i++)
                {
                    await context.Response.Body.WriteAsync(scratchBuffer, 0, scratchBuffer.Length);
                    await Task.Delay(10);
                }

                await requestAborted.Task.DefaultTimeout();
                appCompletedTcs.SetResult(null);
            }, new TestServiceContext(LoggerFactory), listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    // Read just part of the response and close the connection.
                    // https://github.com/aspnet/KestrelHttpServer/issues/2554
                    await connection.Stream.ReadAsync(scratchBuffer, 0, scratchBuffer.Length);

                    connection.Reset();
                }

                await requestAborted.Task.DefaultTimeout();

                // After the RequestAborted token is tripped, the connection reset should be logged.
                // On Linux and macOS, the connection close is still sometimes observed as a FIN despite the LingerState.
                var presShutdownTransportLogs = TestSink.Writes.Where(
                    w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" ||
                         w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
                var connectionResetLogs = presShutdownTransportLogs.Where(
                    w => w.EventId == connectionResetEventId ||
                         (!TestPlatformHelper.IsWindows && w.EventId == connectionFinEventId));

                Assert.NotEmpty(connectionResetLogs);

                // On macOS, the default 5 shutdown timeout is insufficient for the write loop to complete, so give it extra time.
                await appCompletedTcs.Task.DefaultTimeout();
                await server.StopAsync();
            }

            var coreLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel");
            Assert.Single(coreLogs.Where(w => w.EventId == connectionStopEventId));

            var transportLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel" ||
                                                           w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" ||
                                                           w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");

            Assert.Empty(transportLogs.Where(w => w.LogLevel > LogLevel.Debug));
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ClientAbortingConnectionImmediatelyIsNotLoggedHigherThanDebug(ListenOptions listenOptions)
        {
            // Attempt multiple connections to be extra sure the resets are consistently logged appropriately.
            const int numConnections = 10;

            // There's not guarantee that the app even gets invoked in this test. The connection reset can be observed
            // as early as accept.
            using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), listenOptions))
            {
                for (var i = 0; i < numConnections; i++)
                {
                    using (var connection = server.CreateConnection())
                    {
                        await connection.Send(
                            "GET / HTTP/1.1",
                            "Host:",
                            "",
                            "");

                        connection.Reset();
                    }
                }
                await server.StopAsync();
            }

            var transportLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" ||
                                                           w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");

            // The "Microsoft.AspNetCore.Server.Kestrel" logger may contain info level logs because resetting the connection can cause
            // partial headers to be read leading to a bad request.
            var coreLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel");

            Assert.Empty(transportLogs.Where(w => w.LogLevel > LogLevel.Debug));
            Assert.Empty(coreLogs.Where(w => w.LogLevel > LogLevel.Information));
        }

        [Fact]
        public async Task ConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate()
        {
            var logger = LoggerFactory.CreateLogger($"{ typeof(ResponseTests).FullName}.{ nameof(ConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate)}");
            const int chunkSize = 1024;
            const int chunks = 256 * 1024;
            var responseSize = chunks * chunkSize;
            var chunkData = new byte[chunkSize];

            var responseRateTimeoutMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionStopMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appFuncCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.ResponseMinimumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => responseRateTimeoutMessageLogged.SetResult(null));
            mockKestrelTrace
                .Setup(trace => trace.ConnectionStop(It.IsAny<string>()))
                .Callback(() => connectionStopMessageLogged.SetResult(null));

            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)
            {
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 1024 * 1024, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            testContext.InitializeHeartbeat();

            var appLogger = LoggerFactory.CreateLogger("App");
            async Task App(HttpContext context)
            {
                appLogger.LogInformation("Request received");
                context.RequestAborted.Register(() => requestAborted.SetResult(null));

                context.Response.ContentLength = responseSize;

                var i = 0;

                try
                {
                    for (; i < chunks; i++)
                    {
                        await context.Response.BodyWriter.WriteAsync(new Memory<byte>(chunkData, 0, chunkData.Length), context.RequestAborted);
                        await Task.Yield();
                    }

                    appFuncCompleted.SetException(new Exception("This shouldn't be reached."));
                }
                catch (OperationCanceledException)
                {
                    appFuncCompleted.SetResult(null);
                    throw;
                }
                catch (Exception ex)
                {
                    appFuncCompleted.SetException(ex);
                }
                finally
                {
                    appLogger.LogInformation("Wrote {total} bytes", chunkSize * i);
                    await requestAborted.Task.DefaultTimeout();
                }
            }

            using (var server = new TestServer(App, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    logger.LogInformation("Sending request");
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    logger.LogInformation("Sent request");

                    var sw = Stopwatch.StartNew();
                    logger.LogInformation("Waiting for connection to abort.");

                    await requestAborted.Task.DefaultTimeout();
                    await responseRateTimeoutMessageLogged.Task.DefaultTimeout();
                    await connectionStopMessageLogged.Task.DefaultTimeout();
                    await appFuncCompleted.Task.DefaultTimeout();
                    await AssertStreamAborted(connection.Stream, chunkSize * chunks);

                    sw.Stop();
                    logger.LogInformation("Connection was aborted after {totalMilliseconds}ms.", sw.ElapsedMilliseconds);
                }
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task HttpsConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate()
        {
            const int chunkSize = 1024;
            const int chunks = 256 * 1024;
            var chunkData = new byte[chunkSize];

            var certificate = TestResources.GetTestCertificate();

            var responseRateTimeoutMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionStopMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var aborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appFuncCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.ResponseMinimumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => responseRateTimeoutMessageLogged.SetResult(null));
            mockKestrelTrace
                .Setup(trace => trace.ConnectionStop(It.IsAny<string>()))
                .Callback(() => connectionStopMessageLogged.SetResult(null));

            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)
            {
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 1024 * 1024, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            testContext.InitializeHeartbeat();

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters =
                {
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions { ServerCertificate = certificate })
                }
            };

            using (var server = new TestServer(async context =>
            {
                context.RequestAborted.Register(() =>
                {
                    aborted.SetResult(null);
                });

                context.Response.ContentLength = chunks * chunkSize;

                try
                {
                    for (var i = 0; i < chunks; i++)
                    {
                        await context.Response.BodyWriter.WriteAsync(new Memory<byte>(chunkData, 0, chunkData.Length), context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    appFuncCompleted.SetResult(null);
                    throw;
                }
                finally
                {
                    await aborted.Task.DefaultTimeout();
                }
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    using (var sslStream = new SslStream(connection.Stream, false, (sender, cert, chain, errors) => true, null))
                    {
                        await sslStream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);

                        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                        await sslStream.WriteAsync(request, 0, request.Length);

                        await aborted.Task.DefaultTimeout();
                        await responseRateTimeoutMessageLogged.Task.DefaultTimeout();
                        await connectionStopMessageLogged.Task.DefaultTimeout();
                        await appFuncCompleted.Task.DefaultTimeout();

                        await AssertStreamAborted(connection.Stream, chunkSize * chunks);
                    }
                }
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConnectionClosedWhenBothRequestAndResponseExperienceBackPressure()
        {
            const int bufferSize = 65536;
            const int bufferCount = 100;
            var responseSize = bufferCount * bufferSize;
            var buffer = new byte[bufferSize];

            var responseRateTimeoutMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionStopMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var copyToAsyncCts = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.ResponseMinimumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => responseRateTimeoutMessageLogged.SetResult(null));
            mockKestrelTrace
                .Setup(trace => trace.ConnectionStop(It.IsAny<string>()))
                .Callback(() => connectionStopMessageLogged.SetResult(null));

            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)
            {
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 1024 * 1024, gracePeriod: TimeSpan.FromSeconds(2)),
                        MaxRequestBodySize = responseSize
                    }
                }
            };

            testContext.InitializeHeartbeat();

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            async Task App(HttpContext context)
            {
                context.RequestAborted.Register(() =>
                {
                    requestAborted.SetResult(null);
                });

                try
                {
                    await context.Request.Body.CopyToAsync(context.Response.Body);
                }
                catch (Exception ex)
                {
                    copyToAsyncCts.SetException(ex);
                    throw;
                }
                finally
                {
                    await requestAborted.Task.DefaultTimeout();
                }

                copyToAsyncCts.SetException(new Exception("This shouldn't be reached."));
            }

            using (var server = new TestServer(App, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // Close the connection with the last request so AssertStreamCompleted actually completes.
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        $"Content-Length: {responseSize}",
                        "",
                        "");

                    var sendTask = Task.Run(async () =>
                    {
                        for (var i = 0; i < bufferCount; i++)
                        {
                            await connection.Stream.WriteAsync(buffer, 0, buffer.Length);
                            await Task.Delay(10);
                        }
                    });

                    await requestAborted.Task.DefaultTimeout();
                    await responseRateTimeoutMessageLogged.Task.DefaultTimeout();
                    await connectionStopMessageLogged.Task.DefaultTimeout();

                    // Expect OperationCanceledException instead of IOException because the server initiated the abort due to a response rate timeout.
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => copyToAsyncCts.Task).DefaultTimeout();
                    await AssertStreamAborted(connection.Stream, responseSize);
                }
                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ConnectionNotClosedWhenClientSatisfiesMinimumDataRateGivenLargeResponseChunks()
        {
            var chunkSize = 64 * 128 * 1024;
            var chunkCount = 4;
            var chunkData = new byte[chunkSize];

            var requestAborted = false;
            var appFuncCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockKestrelTrace = new Mock<IKestrelTrace>();

            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)
            {
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            testContext.InitializeHeartbeat();

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            async Task App(HttpContext context)
            {
                context.RequestAborted.Register(() =>
                {
                    requestAborted = true;
                });

                for (var i = 0; i < chunkCount; i++)
                {
                    await context.Response.BodyWriter.WriteAsync(new Memory<byte>(chunkData, 0, chunkData.Length), context.RequestAborted);
                }

                appFuncCompleted.SetResult(null);
            }

            using (var server = new TestServer(App, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    // Close the connection with the last request so AssertStreamCompleted actually completes.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: close",
                        "",
                        "");

                    var minTotalOutputSize = chunkCount * chunkSize;

                    // Make sure consuming a single chunk exceeds the 2 second timeout.
                    var targetBytesPerSecond = chunkSize / 4;
                    await AssertStreamCompleted(connection.Stream, minTotalOutputSize, targetBytesPerSecond);
                    await appFuncCompleted.Task.DefaultTimeout();
                }
                await server.StopAsync();
            }

            mockKestrelTrace.Verify(t => t.ResponseMinimumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.Once());
            Assert.False(requestAborted);
        }

        private bool ConnectionNotClosedWhenClientSatisfiesMinimumDataRateGivenLargeResponseHeadersRetryPredicate(Exception e)
            => e is IOException && e.Message.Contains("Unable to read data from the transport connection: The I/O operation has been aborted because of either a thread exit or an application request");

        [Fact]
        [RetryTest(nameof(ConnectionNotClosedWhenClientSatisfiesMinimumDataRateGivenLargeResponseHeadersRetryPredicate),
            "Active investigation into potential corefx sockets bug: https://github.com/dotnet/corefx/issues/30691",
            OperatingSystems.Windows,
            5)]
        public async Task ConnectionNotClosedWhenClientSatisfiesMinimumDataRateGivenLargeResponseHeaders()
        {
            var headerSize = 1024 * 1024; // 1 MB for each header value
            var headerCount = 64; // 64 MB of headers per response
            var requestCount = 4; // Minimum of 256 MB of total response headers
            var headerValue = new string('a', headerSize);
            var headerStringValues = new StringValues(Enumerable.Repeat(headerValue, headerCount).ToArray());

            var requestAborted = false;
            var mockKestrelTrace = new Mock<IKestrelTrace>();

            var testContext = new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)
            {
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            testContext.InitializeHeartbeat();

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            async Task App(HttpContext context)
            {
                context.RequestAborted.Register(() =>
                {
                    requestAborted = true;
                });

                context.Response.Headers[$"X-Custom-Header"] = headerStringValues;
                context.Response.ContentLength = 0;

                await context.Response.BodyWriter.FlushAsync();
            }

            using (var server = new TestServer(App, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    for (var i = 0; i < requestCount - 1; i++)
                    {
                        await connection.Send(
                            "GET / HTTP/1.1",
                            "Host:",
                            "",
                            "");
                    }

                    // Close the connection with the last request so AssertStreamCompleted actually completes.
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: close",
                        "",
                        "");

                    var responseSize = headerSize * headerCount;
                    var minTotalOutputSize = requestCount * responseSize;

                    // Make sure consuming a single set of response headers exceeds the 2 second timeout.
                    var targetBytesPerSecond = responseSize / 4;
                    await AssertStreamCompleted(connection.Stream, minTotalOutputSize, targetBytesPerSecond);
                }
                await server.StopAsync();
            }

            mockKestrelTrace.Verify(t => t.ResponseMinimumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.Once());
            Assert.False(requestAborted);
        }

        private async Task AssertStreamAborted(Stream stream, int totalBytes)
        {
            var receiveBuffer = new byte[64 * 1024];
            var totalReceived = 0;

            try
            {
                while (totalReceived < totalBytes)
                {
                    var bytes = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length).DefaultTimeout();

                    if (bytes == 0)
                    {
                        break;
                    }

                    totalReceived += bytes;
                }
            }
            catch (IOException)
            {
                // This is expected given an abort.
            }

            Assert.True(totalReceived < totalBytes, $"{nameof(AssertStreamAborted)} Stream completed successfully.");
        }

        private async Task AssertStreamCompleted(Stream stream, long minimumBytes, int targetBytesPerSecond)
        {
            var receiveBuffer = new byte[64 * 1024];
            var received = 0;
            var totalReceived = 0;
            var startTime = DateTimeOffset.UtcNow;

            do
            {
                received = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                totalReceived += received;

                var expectedTimeElapsed = TimeSpan.FromSeconds(totalReceived / targetBytesPerSecond);
                var timeElapsed = DateTimeOffset.UtcNow - startTime;
                if (timeElapsed < expectedTimeElapsed)
                {
                    await Task.Delay(expectedTimeElapsed - timeElapsed);
                }
            } while (received > 0);

            Assert.True(totalReceived >= minimumBytes, $"{nameof(AssertStreamCompleted)} Stream aborted prematurely.");
        }

        public static TheoryData<string, StringValues, string> NullHeaderData
        {
            get
            {
                var dataset = new TheoryData<string, StringValues, string>();

                // Unknown headers
                dataset.Add("NullString", (string)null, null);
                dataset.Add("EmptyString", "", "");
                dataset.Add("NullStringArray", new string[] { null }, null);
                dataset.Add("EmptyStringArray", new string[] { "" }, "");
                dataset.Add("MixedStringArray", new string[] { null, "" }, "");
                // Known headers
                dataset.Add("Location", (string)null, null);
                dataset.Add("Location", "", "");
                dataset.Add("Location", new string[] { null }, null);
                dataset.Add("Location", new string[] { "" }, "");
                dataset.Add("Location", new string[] { null, "" }, "");

                return dataset;
            }
        }
    }
}
