// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

using static Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.FinOnErrorHelpers;

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
                            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                        }
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

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
                host.Start();

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
            }
        }

        [Fact]
        public async Task OnCompleteCalledEvenWhenOnStartingNotCalled()
        {
            var onStartingCalled = false;
            var onCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        context.Response.OnStarting(() => Task.Run(() => onStartingCalled = true));
                        context.Response.OnCompleted(() => Task.Run(() =>
                        {
                            onCompletedTcs.SetResult(null);
                        }));

                        // Prevent OnStarting call (see HttpProtocol.ProcessRequestsAsync()).
                        throw new Exception();
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");

                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                    Assert.False(onStartingCalled);
                    await onCompletedTcs.Task.DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task OnStartingThrowsWhenSetAfterResponseHasAlreadyStarted()
        {
            InvalidOperationException ex = null;

            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("hello, world");
                        await context.Response.Body.FlushAsync();
                        ex = Assert.Throws<InvalidOperationException>(() => context.Response.OnStarting(_ => Task.CompletedTask, null));
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");

                    // Despite the error, the response had already started
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.NotNull(ex);
                }
            }
        }

        [Fact]
        public async Task ResponseBodyWriteAsyncCanBeCancelled()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);
            var cts = new CancellationTokenSource();
            var appTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeBlockedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                try
                {
                    await context.Response.WriteAsync("hello", cts.Token).DefaultTimeout();

                    var data = new byte[1024 * 1024 * 10];

                    var timerTask = Task.Delay(TimeSpan.FromSeconds(1));
                    var writeTask = context.Response.Body.WriteAsync(data, 0, data.Length, cts.Token).DefaultTimeout();
                    var completedTask = await Task.WhenAny(writeTask, timerTask);

                    while (completedTask == writeTask)
                    {
                        await writeTask;
                        timerTask = Task.Delay(TimeSpan.FromSeconds(1));
                        writeTask = context.Response.Body.WriteAsync(data, 0, data.Length, cts.Token).DefaultTimeout();
                        completedTask = await Task.WhenAny(writeTask, timerTask);
                    }

                    writeBlockedTcs.TrySetResult(null);

                    await writeTask;
                }
                catch (Exception ex)
                {
                    appTcs.TrySetException(ex);
                    writeBlockedTcs.TrySetException(ex);
                }
                finally
                {
                    appTcs.TrySetResult(null);
                }
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    await connection.Receive($"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "5",
                        "hello");

                    await writeBlockedTcs.Task.DefaultTimeout();

                    cts.Cancel();

                    await Assert.ThrowsAsync<OperationCanceledException>(() => appTcs.Task).DefaultTimeout();
                }
            }
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposeAppException()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
                TestSink,
                LoggerFactory,
                context =>
                {
                    throw new Exception();
                },
                expectedClientStatusCode: HttpStatusCode.InternalServerError,
                expectedServerStatusCode: HttpStatusCode.InternalServerError);
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposeRequestAborted()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
                TestSink,
                LoggerFactory,
                context =>
                {
                    context.Abort();
                    return Task.CompletedTask;
                },
                expectedClientStatusCode: null,
                expectedServerStatusCode: 0);
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposeRequestAbortedAppException()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
                TestSink,
                LoggerFactory,
                context =>
                {
                    context.Abort();
                    throw new Exception();
                },
                expectedClientStatusCode: null,
                expectedServerStatusCode: 0);
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposedRequestMalformed()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
                TestSink,
                LoggerFactory,
                context =>
                {
                    return Task.CompletedTask;
                },
                expectedClientStatusCode: HttpStatusCode.OK,
                expectedServerStatusCode: HttpStatusCode.OK,
                sendMalformedRequest: true);
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposedRequestMalformedRead()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
                TestSink,
                LoggerFactory,
                async context =>
                {
                    await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                },
                expectedClientStatusCode: null,
                expectedServerStatusCode: HttpStatusCode.BadRequest,
                sendMalformedRequest: true);
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposedRequestMalformedReadIgnored()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
                TestSink,
                LoggerFactory,
                async context =>
                {
                    try
                    {
                        await context.Request.Body.ReadAsync(new byte[1], 0, 1);
                    }
                    catch (BadHttpRequestException)
                    {
                    }
                },
                expectedClientStatusCode: HttpStatusCode.OK,
                expectedServerStatusCode: HttpStatusCode.OK,
                sendMalformedRequest: true);
        }

        [Fact]
        public async Task OnCompletedExceptionShouldNotPreventAResponse()
        {
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.OnCompleted(_ => throw new Exception(), null);
                        await context.Response.WriteAsync("hello, world");
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task OnCompletedShouldNotBlockAResponse()
        {
            var delayTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .ConfigureServices(AddTestLogging)
                .Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.OnCompleted(async () =>
                        {
                            await delayTcs.Task;
                        });
                        await context.Response.WriteAsync("hello, world");
                    });
                });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"http://127.0.0.1:{host.GetPort()}/");
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                delayTcs.SetResult(null);
            }
        }

        [Fact]
        public async Task InvalidChunkedEncodingInRequestShouldNotBlockOnCompleted()
        {
            var onCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.OnCompleted(() => Task.Run(() =>
                {
                    onCompletedTcs.SetResult(null);
                }));
                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "gg");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            await onCompletedTcs.Task.DefaultTimeout();
        }

        private static async Task ResponseStatusCodeSetBeforeHttpContextDispose(
            ITestSink testSink,
            ILoggerFactory loggerFactory,
            RequestDelegate handler,
            HttpStatusCode? expectedClientStatusCode,
            HttpStatusCode expectedServerStatusCode,
            bool sendMalformedRequest = false)
        {
            var mockHttpContextFactory = new Mock<IHttpContextFactory>();
            mockHttpContextFactory.Setup(f => f.Create(It.IsAny<IFeatureCollection>()))
                .Returns<IFeatureCollection>(fc => new DefaultHttpContext(fc));

            var disposedTcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            mockHttpContextFactory.Setup(f => f.Dispose(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(c =>
                {
                    disposedTcs.TrySetResult(c.Response.StatusCode);
                });

            using (var server = new TestServer(handler, new TestServiceContext(loggerFactory),
                new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
                services => services.AddSingleton(mockHttpContextFactory.Object)))
            {
                if (!sendMalformedRequest)
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            var response = await client.GetAsync($"http://127.0.0.1:{server.Port}/");
                            Assert.Equal(expectedClientStatusCode, response.StatusCode);
                        }
                        catch
                        {
                            if (expectedClientStatusCode != null)
                            {
                                throw;
                            }
                        }
                    }
                }
                else
                {
                    using (var connection = new TestConnection(server.Port))
                    {
                        await connection.Send(
                            "POST / HTTP/1.1",
                            "Host:",
                            "Transfer-Encoding: chunked",
                            "",
                            "gg");
                        if (expectedClientStatusCode == HttpStatusCode.OK)
                        {
                            await connection.ReceiveForcedEnd(
                                "HTTP/1.1 200 OK",
                                $"Date: {server.Context.DateHeaderValue}",
                                "Content-Length: 0",
                                "",
                                "");
                        }
                        else
                        {
                            await connection.ReceiveForcedEnd(
                                "HTTP/1.1 400 Bad Request",
                                "Connection: close",
                                $"Date: {server.Context.DateHeaderValue}",
                                "Content-Length: 0",
                                "",
                                "");
                        }
                    }
                }

                var disposedStatusCode = await disposedTcs.Task.DefaultTimeout();
                Assert.Equal(expectedServerStatusCode, (HttpStatusCode)disposedStatusCode);
            }

            if (sendMalformedRequest)
            {
                Assert.Contains(testSink.Writes, w => w.EventId.Id == 17 && w.LogLevel == LogLevel.Information && w.Exception is BadHttpRequestException
                    && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
            }
            else
            {
                Assert.DoesNotContain(testSink.Writes, w => w.EventId.Id == 17 && w.LogLevel == LogLevel.Information && w.Exception is BadHttpRequestException
                    && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/pull/1111/files#r80584475 explains the reason for this test.
        [Fact]
        public async Task NoErrorResponseSentWhenAppSwallowsBadRequestException()
        {
            BadHttpRequestException readException = null;

            using (var server = new TestServer(async httpContext =>
            {
                readException = await Assert.ThrowsAsync<BadHttpRequestException>(
                    async () => await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1));
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "gg");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.NotNull(readException);

            Assert.Contains(TestSink.Writes, w => w.EventId.Id == 17 && w.LogLevel == LogLevel.Information && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task TransferEncodingChunkedSetOnUnknownLengthHttp11Response()
        {
            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("hello, ");
                await httpContext.Response.WriteAsync("world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "7",
                        "hello, ",
                        "5",
                        "world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(StatusCodes.Status204NoContent)]
        [InlineData(StatusCodes.Status205ResetContent)]
        [InlineData(StatusCodes.Status304NotModified)]
        public async Task TransferEncodingChunkedNotSetOnNonBodyResponse(int statusCode)
        {
            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.StatusCode = statusCode;
                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 {Encoding.ASCII.GetString(ReasonPhrases.ToStatusBytes(statusCode))}",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task TransferEncodingNotSetOnHeadResponse()
        {
            using (var server = new TestServer(httpContext =>
            {
                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseBodyNotWrittenOnHeadResponseAndLoggedOnlyOnce()
        {
            const string response = "hello, world";

            var logTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.ConnectionHeadResponseBodyWrite(It.IsAny<string>(), response.Length))
                .Callback<string, long>((connectionId, count) => logTcs.SetResult(null));

            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync(response);
                await httpContext.Response.Body.FlushAsync();
            }, new TestServiceContext(LoggerFactory, mockKestrelTrace.Object)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");

                    // Wait for message to be logged before disposing the socket.
                    // Disposing the socket will abort the connection and HttpProtocol._requestAborted
                    // might be 1 by the time ProduceEnd() gets called and the message is logged.
                    await logTcs.Task.DefaultTimeout();
                }
            }

            mockKestrelTrace.Verify(kestrelTrace =>
                kestrelTrace.ConnectionHeadResponseBodyWrite(It.IsAny<string>(), response.Length), Times.Once);
        }

        [Fact]
        public async Task ThrowsAndClosesConnectionWhenAppWritesMoreThanContentLengthWrite()
        {
            var serviceContext = new TestServiceContext(LoggerFactory)
            {
                ServerOptions = { AllowSynchronousIO = true }
            };

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.ContentLength = 11;
                httpContext.Response.Body.Write(Encoding.ASCII.GetBytes("hello,"), 0, 6);
                httpContext.Response.Body.Write(Encoding.ASCII.GetBytes(" world"), 0, 6);
                return Task.CompletedTask;
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "hello,");

                    await connection.WaitForConnectionClose().DefaultTimeout();
                }
            }

            var logMessage = Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);

            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 11).",
                logMessage.Exception.Message);

        }

        [Fact]
        public async Task ThrowsAndClosesConnectionWhenAppWritesMoreThanContentLengthWriteAsync()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("hello,");
                await httpContext.Response.WriteAsync(" world");
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "hello,");
                }
            }

            var logMessage = Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 11).",
                logMessage.Exception.Message);
        }

        [Fact]
        public async Task InternalServerErrorAndConnectionClosedOnWriteWithMoreThanContentLengthAndResponseNotStarted()
        {
            var serviceContext = new TestServiceContext(LoggerFactory)
            {
                ServerOptions = { AllowSynchronousIO = true }
            };

            using (var server = new TestServer(httpContext =>
            {
                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = 5;
                httpContext.Response.Body.Write(response, 0, response.Length);
                return Task.CompletedTask;
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        $"HTTP/1.1 500 Internal Server Error",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            var logMessage = Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 5).",
                logMessage.Exception.Message);
        }

        [Fact]
        public async Task InternalServerErrorAndConnectionClosedOnWriteAsyncWithMoreThanContentLengthAndResponseNotStarted()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(httpContext =>
            {
                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = 5;
                return httpContext.Response.Body.WriteAsync(response, 0, response.Length);
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        $"HTTP/1.1 500 Internal Server Error",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            var logMessage = Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 5).",
                logMessage.Exception.Message);
        }

        [Fact]
        public async Task WhenAppWritesLessThanContentLengthErrorLogged()
        {
            var logTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.ApplicationError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InvalidOperationException>()))
                .Callback<string, string, Exception>((connectionId, requestId, ex) =>
                {
                    logTcs.SetResult(null);
                });

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 13;
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory, mockTrace.Object)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    // Don't use ReceiveEnd here, otherwise the FIN might
                    // abort the request before the server checks the
                    // response content length, in which case the check
                    // will be skipped.
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 13",
                        "",
                        "hello, world");

                    // Wait for error message to be logged.
                    await logTcs.Task.DefaultTimeout();

                    // The server should close the connection in this situation.
                    await connection.WaitForConnectionClose().DefaultTimeout();
                }
            }

            mockTrace.Verify(trace =>
                trace.ApplicationError(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.Is<InvalidOperationException>(ex =>
                        ex.Message.Equals($"Response Content-Length mismatch: too few bytes written (12 of 13).", StringComparison.Ordinal))));
        }

        [Fact]
        public async Task WhenAppWritesLessThanContentLengthButRequestIsAbortedErrorNotLogged()
        {
            var requestAborted = new SemaphoreSlim(0);
            var mockTrace = new Mock<IKestrelTrace>();

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.RequestAborted.Register(() =>
                {
                    requestAborted.Release(2);
                });

                httpContext.Response.ContentLength = 12;
                await httpContext.Response.WriteAsync("hello,");

                // Wait until the request is aborted so we know HttpProtocol will skip the response content length check.
                Assert.True(await requestAborted.WaitAsync(TestConstants.DefaultTimeout));
            }, new TestServiceContext(LoggerFactory, mockTrace.Object)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "hello,");
                }

                // Verify the request was really aborted. A timeout in
                // the app would cause a server error and skip the content length
                // check altogether, making the test pass for the wrong reason.
                // Await before disposing the server to prevent races between the
                // abort triggered by the connection RST and the abort called when
                // disposing the server.
                Assert.True(await requestAborted.WaitAsync(TestConstants.DefaultTimeout));
            }

            // With the server disposed we know all connections were drained and all messages were logged.
            mockTrace.Verify(trace => trace.ApplicationError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InvalidOperationException>()), Times.Never);
        }

        [Fact]
        public async Task WhenAppSetsContentLengthButDoesNotWriteBody500ResponseSentAndConnectionDoesNotClose()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.ContentLength = 5;
                return Task.CompletedTask;
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 500 Internal Server Error",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "HTTP/1.1 500 Internal Server Error",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            var error = TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error);
            Assert.Equal(2, error.Count());
            Assert.All(error, message => message.Message.Equals("Response Content-Length mismatch: too few bytes written (0 of 5)."));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WhenAppSetsContentLengthToZeroAndDoesNotWriteNoErrorIsThrown(bool flushResponse)
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 0;

                if (flushResponse)
                {
                    await httpContext.Response.Body.FlushAsync();
                }
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.Empty(TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error));
        }

        // https://tools.ietf.org/html/rfc7230#section-3.3.3
        // If a message is received with both a Transfer-Encoding and a
        // Content-Length header field, the Transfer-Encoding overrides the
        // Content-Length.
        [Fact]
        public async Task WhenAppSetsTransferEncodingAndContentLengthWritingLessIsNotAnError()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Transfer-Encoding"] = "chunked";
                httpContext.Response.ContentLength = 13;
                await httpContext.Response.WriteAsync("hello, world");
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 13",
                        "Transfer-Encoding: chunked",
                        "",
                        "hello, world");
                }
            }

            Assert.Empty(TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error));
        }

        // https://tools.ietf.org/html/rfc7230#section-3.3.3
        // If a message is received with both a Transfer-Encoding and a
        // Content-Length header field, the Transfer-Encoding overrides the
        // Content-Length.
        [Fact]
        public async Task WhenAppSetsTransferEncodingAndContentLengthWritingMoreIsNotAnError()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Transfer-Encoding"] = "chunked";
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("hello, world");
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        $"HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 11",
                        "Transfer-Encoding: chunked",
                        "",
                        "hello, world");
                }
            }

            Assert.Empty(TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error));
        }

        [Fact]
        public async Task HeadResponseCanContainContentLengthHeader()
        {
            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.ContentLength = 42;
                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 42",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task HeadResponseBodyNotWrittenWithAsyncWrite()
        {
            var flushed = new SemaphoreSlim(0, 1);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                await httpContext.Response.WriteAsync("hello, world");
                await flushed.WaitAsync();
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "");

                    flushed.Release();
                }
            }
        }

        [Fact]
        public async Task HeadResponseBodyNotWrittenWithSyncWrite()
        {
            var flushed = new SemaphoreSlim(0, 1);
            var serviceContext = new TestServiceContext(LoggerFactory) { ServerOptions = { AllowSynchronousIO = true } };

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                httpContext.Response.Body.Write(Encoding.ASCII.GetBytes("hello, world"), 0, 12);
                await flushed.WaitAsync();
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "");

                    flushed.Release();
                }
            }
        }

        [Fact]
        public async Task ZeroLengthWritesFlushHeaders()
        {
            var flushed = new SemaphoreSlim(0, 1);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                await httpContext.Response.WriteAsync("");
                await flushed.WaitAsync();
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "");

                    flushed.Release();

                    await connection.ReceiveEnd("hello, world");
                }
            }
        }

        [Fact]
        public async Task WriteAfterConnectionCloseNoops()
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
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    await requestStarted.Task.DefaultTimeout();
                    connection.Shutdown(SocketShutdown.Send);
                    await connection.WaitForConnectionClose().DefaultTimeout();
                }

                connectionClosed.SetResult(null);

                await appCompleted.Task.DefaultTimeout();
            }
        }

        [Fact]
        public async Task AppCanWriteOwnBadRequestResponse()
        {
            var expectedResponse = string.Empty;
            var responseWritten = new SemaphoreSlim(0);

            using (var server = new TestServer(async httpContext =>
            {
                try
                {
                    await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
                }
                catch (BadHttpRequestException ex)
                {
                    expectedResponse = ex.Message;
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    httpContext.Response.ContentLength = ex.Message.Length;
                    await httpContext.Response.WriteAsync(ex.Message);
                    responseWritten.Release();
                }
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "gg");
                    await responseWritten.WaitAsync().DefaultTimeout();
                    await connection.ReceiveEnd(
                        "HTTP/1.1 400 Bad Request",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Content-Length: {expectedResponse.Length}",
                        "",
                        expectedResponse);
                }
            }
        }

        [Theory]
        [InlineData("gzip")]
        [InlineData("chunked, gzip")]
        public async Task ConnectionClosedWhenChunkedIsNotFinalTransferCoding(string responseTransferEncoding)
        {
            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Transfer-Encoding"] = responseTransferEncoding;
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: {responseTransferEncoding}",
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
                        "HTTP/1.1 200 OK",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: {responseTransferEncoding}",
                        "",
                        "hello, world");
                }
            }
        }

        [Theory]
        [InlineData("gzip")]
        [InlineData("chunked, gzip")]
        public async Task ConnectionClosedWhenChunkedIsNotFinalTransferCodingEvenIfConnectionKeepAliveSetInResponse(string responseTransferEncoding)
        {
            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Connection"] = "keep-alive";
                httpContext.Response.Headers["Transfer-Encoding"] = responseTransferEncoding;
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: {responseTransferEncoding}",
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
                        "HTTP/1.1 200 OK",
                        "Connection: keep-alive",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: {responseTransferEncoding}",
                        "",
                        "hello, world");
                }
            }
        }

        [Theory]
        [InlineData("chunked")]
        [InlineData("gzip, chunked")]
        public async Task ConnectionKeptAliveWhenChunkedIsFinalTransferCoding(string responseTransferEncoding)
        {
            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Transfer-Encoding"] = responseTransferEncoding;

                // App would have to chunk manually, but here we don't care
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: {responseTransferEncoding}",
                        "",
                        "hello, world");

                    // Make sure connection was kept open
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: {responseTransferEncoding}",
                        "",
                        "hello, world");
                }
            }
        }

        [Fact]
        public async Task FirstWriteVerifiedAfterOnStarting()
        {
            var serviceContext = new TestServiceContext(LoggerFactory) { ServerOptions = { AllowSynchronousIO = true } };

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.OnStarting(() =>
                {
                    // Change response to chunked
                    httpContext.Response.ContentLength = null;
                    return Task.CompletedTask;
                });

                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = response.Length - 1;

                // If OnStarting is not run before verifying writes, an error response will be sent.
                httpContext.Response.Body.Write(response, 0, response.Length);
                return Task.CompletedTask;
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task SubsequentWriteVerifiedAfterOnStarting()
        {
            var serviceContext = new TestServiceContext(LoggerFactory) { ServerOptions = { AllowSynchronousIO = true } };

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.OnStarting(() =>
                {
                    // Change response to chunked
                    httpContext.Response.ContentLength = null;
                    return Task.CompletedTask;
                });

                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = response.Length - 1;

                // If OnStarting is not run before verifying writes, an error response will be sent.
                httpContext.Response.Body.Write(response, 0, response.Length / 2);
                httpContext.Response.Body.Write(response, response.Length / 2, response.Length - response.Length / 2);
                return Task.CompletedTask;
            }, serviceContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: chunked",
                        "",
                        "6",
                        "hello,",
                        "6",
                        " world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task FirstWriteAsyncVerifiedAfterOnStarting()
        {
            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.OnStarting(() =>
                {
                    // Change response to chunked
                    httpContext.Response.ContentLength = null;
                    return Task.CompletedTask;
                });

                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = response.Length - 1;

                // If OnStarting is not run before verifying writes, an error response will be sent.
                return httpContext.Response.Body.WriteAsync(response, 0, response.Length);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task SubsequentWriteAsyncVerifiedAfterOnStarting()
        {
            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.OnStarting(() =>
                {
                    // Change response to chunked
                    httpContext.Response.ContentLength = null;
                    return Task.CompletedTask;
                });

                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = response.Length - 1;

                // If OnStarting is not run before verifying writes, an error response will be sent.
                await httpContext.Response.Body.WriteAsync(response, 0, response.Length / 2);
                await httpContext.Response.Body.WriteAsync(response, response.Length / 2, response.Length - response.Length / 2);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: chunked",
                        "",
                        "6",
                        "hello,",
                        "6",
                        " world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task WhenResponseAlreadyStartedResponseEndedBeforeConsumingRequestBody()
        {
            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 1",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "");

                    // If the expected behavior is regressed, this will hang because the
                    // server will try to consume the request body before flushing the chunked
                    // terminator.
                    await connection.Receive(
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task WhenResponseNotStartedResponseEndedBeforeConsumingRequestBody()
        {
            using (var server = new TestServer(httpContext => Task.CompletedTask,
                new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "gg");

                    // This will receive a success response because the server flushed the response
                    // before reading the malformed chunk header in the request, but then it will close
                    // the connection.
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, w => w.EventId.Id == 17 && w.LogLevel == LogLevel.Information && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task RequestDrainingFor100ContinueDoesNotBlockResponse()
        {
            var foundMessage = false;
            using (var server = new TestServer(httpContext =>
            {
                return httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "Expect: 100-continue",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 100 Continue",
                        "",
                        "");

                    // Let the app finish
                    await connection.Send(
                        "1",
                        "a",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");

                    // This will be consumed by Http1Connection when it attempts to
                    // consume the request body and will cause an error.
                    await connection.Send(
                        "gg");

                    // Wait for the server to drain the request body and log an error.
                    // Time out after 10 seconds
                    for (int i = 0; i < 10 && !foundMessage; i++)
                    {
                        while (TestApplicationErrorLogger.Messages.TryDequeue(out var message))
                        {
                            if (message.EventId.Id == 17 && message.LogLevel == LogLevel.Information && message.Exception is BadHttpRequestException
                                && ((BadHttpRequestException)message.Exception).StatusCode == StatusCodes.Status400BadRequest)
                            {
                                foundMessage = true;
                                break;
                            }
                        }

                        if (!foundMessage)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                    }

                    await connection.ReceiveEnd();
                }
            }

            Assert.True(foundMessage, "Expected log not found");
        }

        [Fact]
        public async Task Sending100ContinueDoesNotPreventAutomatic400Responses()
        {
            using (var server = new TestServer(httpContext =>
            {
                return httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "Expect: 100-continue",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 100 Continue",
                        "",
                        "");

                    // Send an invalid chunk prefix to cause an error.
                    await connection.Send(
                        "gg");

                    // If 100 Continue sets HttpProtocol.HasResponseStarted to true,
                    // a success response will be produced before the server sees the
                    // bad chunk header above, making this test fail.
                    await connection.ReceiveForcedEnd(
                        "HTTP/1.1 400 Bad Request",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.Contains(TestApplicationErrorLogger.Messages, w => w.EventId.Id == 17 && w.LogLevel == LogLevel.Information && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task Sending100ContinueAndResponseSendsChunkTerminatorBeforeConsumingRequestBody()
        {
            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
                await httpContext.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 2",
                        "Expect: 100-continue",
                        "",
                        "");

                    await connection.Receive(
                        "HTTP/1.1 100 Continue",
                        "",
                        "");

                    await connection.Send(
                        "a");

                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        $"Transfer-Encoding: chunked",
                        "",
                        "c",
                        "hello, world",
                        "");

                    // If the expected behavior is regressed, this will hang because the
                    // server will try to consume the request body before flushing the chunked
                    // terminator.
                    await connection.Receive(
                        "0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task Http11ResponseSentToHttp10Request(ListenOptions listenOptions)
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
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
                        $"Date: {serviceContext.DateHeaderValue}",
                        "",
                        "Hello World");
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroContentLengthSetAutomaticallyAfterNoWrites(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EmptyApp, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroContentLengthSetAutomaticallyForNonKeepAliveRequests(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForHeadRequests(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EmptyApp, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "HEAD / HTTP/1.1",
                        "Host:",
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var request = httpContext.Request;
                var response = httpContext.Response;

                using (var reader = new StreamReader(request.Body, Encoding.ASCII))
                {
                    var statusString = await reader.ReadLineAsync();
                    response.StatusCode = int.Parse(statusString);
                }
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 3",
                        "",
                        "204POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 3",
                        "",
                        "205POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 3",
                        "",
                        "304POST / HTTP/1.1",
                        "Host:",
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ConnectionClosedAfter101Response(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var request = httpContext.Request;
                var stream = await httpContext.Features.Get<IHttpUpgradeFeature>().UpgradeAsync();
                var response = Encoding.ASCII.GetBytes("hello, world");
                await stream.WriteAsync(response, 0, response.Length);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "Connection: Upgrade",
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
                        "Connection: keep-alive, Upgrade",
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingResultsIn500Response(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            bool onStartingCalled = false;

            using (var server = new TestServer(httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.CompletedTask;
                }, null);

                // Anything added to the ResponseHeaders dictionary is ignored
                response.Headers["Content-Length"] = "11";
                throw new Exception();
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "GET / HTTP/1.1",
                        "Host:",
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
            Assert.Equal(2, TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error).Count());
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingInOnStartingResultsInFailedWritesAnd500Response(ListenOptions listenOptions)
        {
            var callback1Called = false;
            var callback2CallCount = 0;

            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var onStartingException = new Exception();

                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    callback1Called = true;
                    throw onStartingException;
                }, null);
                response.OnStarting(_ =>
                {
                    callback2CallCount++;
                    throw onStartingException;
                }, null);

                var writeException = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await response.Body.FlushAsync());
                Assert.Same(onStartingException, writeException.InnerException);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "GET / HTTP/1.1",
                        "Host:",
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

            // The first registered OnStarting callback should have been called,
            // since they are called LIFO order and the other one failed.
            Assert.False(callback1Called);
            Assert.Equal(2, callback2CallCount);
            Assert.Equal(2, TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error).Count());
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingInOnCompletedIsLogged(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            var onCompletedCalled1 = false;
            var onCompletedCalled2 = false;

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
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 11",
                        "",
                        "Hello World");
                }
            }

            // All OnCompleted callbacks should be called even if they throw.
            Assert.Equal(2, TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error).Count());
            Assert.True(onCompletedCalled1);
            Assert.True(onCompletedCalled2);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingAfterWritingKillsConnection(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            bool onStartingCalled = false;

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
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
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
            Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingAfterPartialWriteKillsConnection(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            bool onStartingCalled = false;

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
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
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
            Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
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
                    await response.WriteAsync(largeString, lifetime.RequestAborted);
                }
                catch (Exception ex)
                {
                    writeTcs.SetException(ex);
                    throw;
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
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task WritingToConnectionAfterUnobservedCloseTriggersRequestAbortedToken(ListenOptions listenOptions)
        {
            const int connectionPausedEventId = 4;
            const int maxRequestBufferSize = 2048;

            var requestAborted = false;
            var readCallbackUnwired = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientClosedConnection = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockKestrelTrace = new Mock<KestrelTrace>(Logger) { CallBase = true };
            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Func<object, Exception, string>>((logLevel, eventId, state, exception, formatter) =>
                {
                    if (eventId.Id == connectionPausedEventId)
                    {
                        readCallbackUnwired.TrySetResult(null);
                    }

                    Logger.Log(logLevel, eventId, state, exception, formatter);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(Logger);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsIn("Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv",
                                                               "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")))
                .Returns(mockLogger.Object);

            var testContext = new TestServiceContext(mockLoggerFactory.Object)
            {
                Log = mockKestrelTrace.Object,
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
                context.RequestAborted.Register(() =>
                {
                    requestAborted = true;
                });

                await clientClosedConnection.Task;

                try
                {
                    for (var i = 0; i < 1000; i++)
                    {
                        await context.Response.Body.WriteAsync(scratchBuffer, 0, scratchBuffer.Length, context.RequestAborted);
                        await Task.Delay(10);
                    }
                }
                catch (Exception ex)
                {
                    writeTcs.SetException(ex);
                    throw;
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
            }

            mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.AtMostOnce());
            Assert.True(requestAborted);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "macOS EPIPE vs. EPROTOTYPE bug https://github.com/aspnet/KestrelHttpServer/issues/2885")]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task AppCanHandleClientAbortingConnectionMidResponse(ListenOptions listenOptions)
        {
            const int connectionResetEventId = 19;
            const int connectionFinEventId = 6;
            //const int connectionStopEventId = 2;

            const int responseBodySegmentSize = 65536;
            const int responseBodySegmentCount = 100;

            var appCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestAborted = false;

            var scratchBuffer = new byte[responseBodySegmentSize];

            using (var server = new TestServer(async context =>
            {
                context.RequestAborted.Register(() =>
                {
                    requestAborted = true;
                });

                for (var i = 0; i < responseBodySegmentCount; i++)
                {
                    await context.Response.Body.WriteAsync(scratchBuffer, 0, scratchBuffer.Length);
                    await Task.Delay(10);
                }

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

                await appCompletedTcs.Task.DefaultTimeout();

                // After the app is done with the write loop, the connection reset should be logged.
                // On Linux and macOS, the connection close is still sometimes observed as a FIN despite the LingerState.
                var presShutdownTransportLogs = TestSink.Writes.Where(
                    w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" ||
                         w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
                var connectionResetLogs = presShutdownTransportLogs.Where(
                    w => w.EventId == connectionResetEventId ||
                         (!TestPlatformHelper.IsWindows && w.EventId == connectionFinEventId));

                Assert.NotEmpty(connectionResetLogs);
            }

            // TODO: Figure out what the following assertion is flaky. The server shouldn't shutdown before all
            // the connections are closed, yet sometimes the connection stop log isn't observed here.
            //var coreLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel");
            //Assert.Single(coreLogs.Where(w => w.EventId == connectionStopEventId));

            Assert.True(requestAborted, "RequestAborted token didn't fire.");

            var transportLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" ||
                                                           w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            Assert.Empty(transportLogs.Where(w => w.LogLevel > LogLevel.Debug));
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "macOS EPIPE vs. EPROTOTYPE bug https://github.com/aspnet/KestrelHttpServer/issues/2885")]
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
            }

            var transportLogs = TestSink.Writes.Where(w => w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv" ||
                                                           w.LoggerName == "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");

            Assert.Empty(transportLogs.Where(w => w.LogLevel > LogLevel.Debug));
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task NoErrorsLoggedWhenServerEndsConnectionBeforeClient(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Content-Length"] = new[] { "11" };
                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello World"), 0, 11);
            }, testContext, listenOptions))
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

            Assert.Empty(TestApplicationErrorLogger.Messages.Where(message => message.LogLevel == LogLevel.Error));
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task NoResponseSentWhenConnectionIsClosedByServerBeforeClientFinishesSendingRequest(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Abort();
                return Task.CompletedTask;
            }, testContext, listenOptions))
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
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ResponseHeadersAreResetOnEachRequest(ListenOptions listenOptions)
        {
            var testContext= new TestServiceContext(LoggerFactory);

            IHeaderDictionary originalResponseHeaders = null;
            var firstRequest = true;

            using (var server = new TestServer(httpContext =>
            {
                var responseFeature = httpContext.Features.Get<IHttpResponseFeature>();

                if (firstRequest)
                {
                    originalResponseHeaders = responseFeature.Headers;
                    responseFeature.Headers = new HttpResponseHeaders();
                    firstRequest = false;
                }
                else
                {
                    Assert.Same(originalResponseHeaders, responseFeature.Headers);
                }

                return Task.CompletedTask;
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "GET / HTTP/1.1",
                        "Host:",
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
        public async Task OnStartingCallbacksAreCalledInLastInFirstOutOrder(ListenOptions listenOptions)
        {
            const string response = "hello, world";

            var testContext= new TestServiceContext(LoggerFactory);

            var callOrder = new Stack<int>();
            var onStartingTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                context.Response.OnStarting(_ =>
                {
                    callOrder.Push(1);
                    onStartingTcs.SetResult(null);
                    return Task.CompletedTask;
                }, null);
                context.Response.OnStarting(_ =>
                {
                    callOrder.Push(2);
                    return Task.CompletedTask;
                }, null);

                context.Response.ContentLength = response.Length;
                await context.Response.WriteAsync(response);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        $"Content-Length: {response.Length}",
                        "",
                        "hello, world");

                    // Wait for all callbacks to be called.
                    await onStartingTcs.Task.DefaultTimeout();
                }
            }

            Assert.Equal(1, callOrder.Pop());
            Assert.Equal(2, callOrder.Pop());
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task OnCompletedCallbacksAreCalledInLastInFirstOutOrder(ListenOptions listenOptions)
        {
            const string response = "hello, world";

            var testContext= new TestServiceContext(LoggerFactory);

            var callOrder = new Stack<int>();
            var onCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                context.Response.OnCompleted(_ =>
                {
                    callOrder.Push(1);
                    onCompletedTcs.SetResult(null);
                    return Task.CompletedTask;
                }, null);
                context.Response.OnCompleted(_ =>
                {
                    callOrder.Push(2);
                    return Task.CompletedTask;
                }, null);

                context.Response.ContentLength = response.Length;
                await context.Response.WriteAsync(response);
            }, testContext, listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        $"Content-Length: {response.Length}",
                        "",
                        "hello, world");

                    // Wait for all callbacks to be called.
                    await onCompletedTcs.Task.DefaultTimeout();
                }
            }

            Assert.Equal(1, callOrder.Pop());
            Assert.Equal(2, callOrder.Pop());
        }


        [Fact]
        public async Task SynchronousWritesAllowedByDefault()
        {
            var firstRequest = true;

            using (var server = new TestServer(async context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.True(bodyControlFeature.AllowSynchronousIO);

                context.Response.ContentLength = 6;

                if (firstRequest)
                {
                    context.Response.Body.Write(Encoding.ASCII.GetBytes("Hello1"), 0, 6);
                    firstRequest = false;
                }
                else
                {
                    bodyControlFeature.AllowSynchronousIO = false;

                    // Synchronous writes now throw.
                    var ioEx = Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(Encoding.ASCII.GetBytes("What!?"), 0, 6));
                    Assert.Equal(CoreStrings.SynchronousWritesDisallowed, ioEx.Message);

                    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello2"), 0, 6);
                }
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.SendEmptyGet();
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 6",
                        "",
                        "Hello1");

                    await connection.SendEmptyGet();
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 6",
                        "",
                        "Hello2");
                }
            }
        }

        [Fact]
        public async Task SynchronousWritesCanBeDisallowedGlobally()
        {
            var testContext = new TestServiceContext(LoggerFactory)
            {
                ServerOptions = { AllowSynchronousIO = false }
            };

            using (var server = new TestServer(context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.False(bodyControlFeature.AllowSynchronousIO);

                context.Response.ContentLength = 6;

                // Synchronous writes now throw.
                var ioEx = Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(Encoding.ASCII.GetBytes("What!?"), 0, 6));
                Assert.Equal(CoreStrings.SynchronousWritesDisallowed, ioEx.Message);

                return context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello!"), 0, 6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 6",
                        "",
                        "Hello!");
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate(bool fin)
        {
            using (StartLog(out var loggerFactory, "ConnClosedWhenRespDoesNotSatisfyMin"))
            {
                var logger = loggerFactory.CreateLogger($"{ typeof(ResponseTests).FullName}.{ nameof(ConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate)}");
                const int chunkSize = 1024;
                const int chunks = 256 * 1024;
                var responseSize = chunks * chunkSize;
                var chunkData = new byte[chunkSize];

                var responseRateTimeoutMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var connectionStopMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var connectionWriteFinMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var connectionWriteRstMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var appFuncCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                var mockLogger = new Mock<ILogger>();
                mockLogger
                    .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
                    .Returns(true);
                mockLogger
                    .Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()))
                    .Callback<LogLevel, EventId, object, Exception, Func<object, Exception, string>>((logLevel, eventId, state, exception, formatter) =>
                    {
                        if (eventId.Name == "ResponseMininumDataRateNotSatisfied")
                        {
                            responseRateTimeoutMessageLogged.TrySetResult(null);
                        }
                        else if (eventId.Name == "ConnectionStop")
                        {
                            connectionStopMessageLogged.TrySetResult(null);
                        }
                        else if (eventId.Name == "ConnectionWriteFin")
                        {
                            connectionWriteFinMessageLogged.TrySetResult(null);
                        }
                        else if (eventId.Name == "ConnectionWriteRst")
                        {
                            connectionWriteRstMessageLogged.TrySetResult(null);
                        }

                        Logger.Log(logLevel, eventId, state, exception, formatter);
                    });

                var mockLoggerFactory = new Mock<ILoggerFactory>();
                mockLoggerFactory
                    .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                    .Returns(Logger);
                mockLoggerFactory
                    .Setup(factory => factory.CreateLogger(It.IsIn("Microsoft.AspNetCore.Server.Kestrel",
                                                                   "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv",
                                                                   "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")))
                    .Returns(mockLogger.Object);

                var testContext = new TestServiceContext(mockLoggerFactory.Object)
                {
                    SystemClock = new SystemClock(),
                    ServerOptions =
                    {
                        Limits =
                        {
                            MinResponseDataRate = new MinDataRate(bytesPerSecond: 1024 * 1024, gracePeriod: TimeSpan.FromSeconds(2))
                        }
                    }
                };

                var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
                listenOptions.ConnectionAdapters.Add(new LoggingConnectionAdapter(loggerFactory.CreateLogger<LoggingConnectionAdapter>()));

                var appLogger = loggerFactory.CreateLogger("App");
                async Task App(HttpContext context)
                {
                    appLogger.LogInformation("Request received");
                    context.RequestAborted.Register(() => requestAborted.SetResult(null));

                    context.Response.ContentLength = responseSize;

                    try
                    {
                        for (var i = 0; i < chunks; i++)
                        {
                            await context.Response.Body.WriteAsync(chunkData, 0, chunkData.Length, context.RequestAborted);
                            appLogger.LogInformation("Wrote chunk of {chunkSize} bytes", chunkSize);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        appFuncCompleted.SetResult(null);
                        throw;
                    }
                }

                using (var server = new TestServer(App, testContext, configureListenOptions: _ => { }, services => SetFinOnError(services, fin)))
                {
                    using (var connection = server.CreateConnection())
                    {
                        try
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
                            if (ExpectFinOnError(fin))
                            {
                                await connectionWriteFinMessageLogged.Task.DefaultTimeout();
                            }
                            else
                            {
                                await connectionWriteRstMessageLogged.Task.DefaultTimeout();
                            }
                            await appFuncCompleted.Task.DefaultTimeout();
                            await AssertStreamAborted(connection.Reader.BaseStream, chunkSize * chunks);

                            sw.Stop();
                            logger.LogInformation("Connection was aborted after {totalMilliseconds}ms.", sw.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Unexpected exception.");
                            throw;
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HttpsConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate(bool fin)
        {
            const int chunkSize = 1024;
            const int chunks = 256 * 1024;
            var chunkData = new byte[chunkSize];

            var certificate = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");

            var responseRateTimeoutMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionStopMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionWriteFinMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionWriteRstMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var aborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var appFuncCompleted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                .Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Func<object, Exception, string>>((logLevel, eventId, state, exception, formatter) =>
                {
                    if (eventId.Name == "ResponseMininumDataRateNotSatisfied")
                    {
                        responseRateTimeoutMessageLogged.TrySetResult(null);
                    }
                    else if (eventId.Name == "ConnectionStop")
                    {
                        connectionStopMessageLogged.TrySetResult(null);
                    }
                    else if (eventId.Name == "ConnectionWriteFin")
                    {
                        connectionWriteFinMessageLogged.TrySetResult(null);
                    }
                    else if (eventId.Name == "ConnectionWriteRst")
                    {
                        connectionWriteRstMessageLogged.TrySetResult(null);
                    }

                    Logger.Log(logLevel, eventId, state, exception, formatter);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(Logger);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsIn("Microsoft.AspNetCore.Server.Kestrel",
                                                               "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv",
                                                               "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")))
                .Returns(mockLogger.Object);

            var testContext = new TestServiceContext(mockLoggerFactory.Object)
            {
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 1024 * 1024, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.ConnectionAdapters.Add(
                    new HttpsConnectionAdapter(new HttpsConnectionAdapterOptions { ServerCertificate = certificate }));
            }

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
                        await context.Response.Body.WriteAsync(chunkData, 0, chunkData.Length, context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    appFuncCompleted.SetResult(null);
                    throw;
                }
            }, testContext, ConfigureListenOptions,
            services => SetFinOnError(services, fin)))
            {
                using (var connection = server.CreateConnection())
                {
                    using (var sslStream = new SslStream(connection.Reader.BaseStream, false, (sender, cert, chain, errors) => true, null))
                    {
                        try
                        {

                            await sslStream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);

                            var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                            await sslStream.WriteAsync(request, 0, request.Length);

                            await aborted.Task.DefaultTimeout();
                            await responseRateTimeoutMessageLogged.Task.DefaultTimeout();
                            await connectionStopMessageLogged.Task.DefaultTimeout();
                            if (ExpectFinOnError(fin))
                            {
                                await connectionWriteFinMessageLogged.Task.DefaultTimeout();
                            }
                            else
                            {
                                await connectionWriteRstMessageLogged.Task.DefaultTimeout();
                            }
                            await appFuncCompleted.Task.DefaultTimeout();

                            // Temporary workaround for a deadlock when reading from an aborted client SslStream on Mac and Linux.
                            if (TestPlatformHelper.IsWindows)
                            {
                                await AssertStreamAborted(sslStream, chunkSize * chunks);
                            }
                            else
                            {
                                await AssertStreamAborted(connection.Reader.BaseStream, chunkSize * chunks);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Unexpected exception.");
                            throw;
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectionClosedWhenBothRequestAndResponseExperienceBackPressure(bool fin)
        {
            const int bufferSize = 65536;
            const int bufferCount = 100;
            var responseSize = bufferCount * bufferSize;
            var buffer = new byte[bufferSize];

            var responseRateTimeoutMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionStopMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionWriteFinMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionWriteRstMessageLogged = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var copyToAsyncCts = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var mockLogger = new Mock<ILogger>();
            mockLogger
                .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);
            mockLogger
                .Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()))
                .Callback<LogLevel, EventId, object, Exception, Func<object, Exception, string>>((logLevel, eventId, state, exception, formatter) =>
                {
                    if (eventId.Name == "ResponseMininumDataRateNotSatisfied")
                    {
                        responseRateTimeoutMessageLogged.TrySetResult(null);
                    }
                    else if (eventId.Name == "ConnectionStop")
                    {
                        connectionStopMessageLogged.TrySetResult(null);
                    }
                    else if (eventId.Name == "ConnectionWriteFin")
                    {
                        connectionWriteFinMessageLogged.TrySetResult(null);
                    }
                    else if (eventId.Name == "ConnectionWriteRst")
                    {
                        connectionWriteRstMessageLogged.TrySetResult(null);
                    }

                    Logger.Log(logLevel, eventId, state, exception, formatter);
                });

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
                .Returns(Logger);
            mockLoggerFactory
                .Setup(factory => factory.CreateLogger(It.IsIn("Microsoft.AspNetCore.Server.Kestrel",
                                                               "Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv",
                                                               "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets")))
                .Returns(mockLogger.Object);

            var testContext = new TestServiceContext(mockLoggerFactory.Object)
            {
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 1024 * 1024, gracePeriod: TimeSpan.FromSeconds(2)),
                        MaxRequestBodySize = responseSize
                    }
                }
            };

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

                copyToAsyncCts.SetException(new Exception("This shouldn't be reached."));
            }

            using (var server = new TestServer(App, testContext, configureListenOptions: _ => { }, services => SetFinOnError(services, fin)))
            {
                using (var connection = server.CreateConnection())
                {
                    try
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
                        if (ExpectFinOnError(fin))
                        {
                            await connectionWriteFinMessageLogged.Task.DefaultTimeout();
                        }
                        else
                        {
                            await connectionWriteRstMessageLogged.Task.DefaultTimeout();
                        }

                        // Expect OperationCanceledException instead of IOException because the server initiated the abort due to a response rate timeout.
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => copyToAsyncCts.Task).DefaultTimeout();
                        await AssertStreamAborted(connection.Stream, responseSize);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Unexpected exception.");
                        throw;
                    }
                }
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

            var testContext = new TestServiceContext
            {
                Log = mockKestrelTrace.Object,
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            async Task App(HttpContext context)
            {
                context.RequestAborted.Register(() =>
                {
                    requestAborted = true;
                });

                for (var i = 0; i < chunkCount; i++)
                {
                    await context.Response.Body.WriteAsync(chunkData, 0, chunkData.Length, context.RequestAborted);
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
                    await AssertStreamCompleted(connection.Reader.BaseStream, minTotalOutputSize, targetBytesPerSecond);
                    await appFuncCompleted.Task.DefaultTimeout();

                    mockKestrelTrace.Verify(t => t.ResponseMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                    mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.AtMostOnce());
                    Assert.False(requestAborted);
                }
            }
        }

        [Fact]
        public async Task ConnectionNotClosedWhenClientSatisfiesMinimumDataRateGivenLargeResponseHeaders()
        {
            var headerSize = 1024 * 1024; // 1 MB for each header value
            var headerCount = 64; // 64 MB of headers per response
            var requestCount = 4; // Minimum of 256 MB of total response headers
            var headerValue = new string('a', headerSize);
            var headerStringValues = new StringValues(Enumerable.Repeat(headerValue, headerCount).ToArray());

            var requestAborted = false;
            var mockKestrelTrace = new Mock<IKestrelTrace>();

            var testContext = new TestServiceContext
            {
                Log = mockKestrelTrace.Object,
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: 240, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            async Task App(HttpContext context)
            {
                context.RequestAborted.Register(() =>
                {
                    requestAborted = true;
                });

                context.Response.Headers[$"X-Custom-Header"] = headerStringValues;
                context.Response.ContentLength = 0;

                await context.Response.Body.FlushAsync();
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
                    await AssertStreamCompleted(connection.Reader.BaseStream, minTotalOutputSize, targetBytesPerSecond);

                    mockKestrelTrace.Verify(t => t.ResponseMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
                    mockKestrelTrace.Verify(t => t.ConnectionStop(It.IsAny<string>()), Times.AtMostOnce());
                    Assert.False(requestAborted);
                }
            }
        }

        [Fact]
        public async Task NonZeroContentLengthFor304StatusCodeIsAllowed()
        {
            using (var server = new TestServer(httpContext =>
            {
                var response = httpContext.Response;
                response.StatusCode = StatusCodes.Status304NotModified;
                response.ContentLength = 42;

                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 304 Not Modified",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 42",
                        "",
                        "");
                }
            }
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
