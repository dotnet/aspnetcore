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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class ResponseTests : LoggedTest
    {
        public static TheoryData<ListenOptions> ConnectionAdapterData => new TheoryData<ListenOptions>
        {
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
            {
                ConnectionAdapters = { new PassThroughConnectionAdapter() }
            }
        };

        public ResponseTests(ITestOutputHelper outputHelper) : base(outputHelper) { }

        [Fact]
        public async Task LargeDownload()
        {
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
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
            var onCompletedCalled = false;

            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel()
                .UseUrls("http://127.0.0.1:0/")
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        context.Response.OnStarting(() => Task.Run(() => onStartingCalled = true));
                        context.Response.OnCompleted(() => Task.Run(() => onCompletedCalled = true));

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
                    Assert.True(onCompletedCalled);
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
        public Task ResponseStatusCodeSetBeforeHttpContextDisposeAppException()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
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
                context =>
                {
                    return Task.CompletedTask;
                },
                expectedClientStatusCode: null,
                expectedServerStatusCode: HttpStatusCode.BadRequest,
                sendMalformedRequest: true);
        }

        [Fact]
        public Task ResponseStatusCodeSetBeforeHttpContextDisposedRequestMalformedRead()
        {
            return ResponseStatusCodeSetBeforeHttpContextDispose(
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
                expectedClientStatusCode: null,
                expectedServerStatusCode: HttpStatusCode.BadRequest,
                sendMalformedRequest: true);
        }

        private static async Task ResponseStatusCodeSetBeforeHttpContextDispose(
            RequestDelegate handler,
            HttpStatusCode? expectedClientStatusCode,
            HttpStatusCode expectedServerStatusCode,
            bool sendMalformedRequest = false)
        {
            var mockHttpContextFactory = new Mock<IHttpContextFactory>();
            mockHttpContextFactory.Setup(f => f.Create(It.IsAny<IFeatureCollection>()))
                .Returns<IFeatureCollection>(fc => new DefaultHttpContext(fc));

            var disposedTcs = new TaskCompletionSource<int>();
            mockHttpContextFactory.Setup(f => f.Dispose(It.IsAny<HttpContext>()))
                .Callback<HttpContext>(c =>
                {
                    disposedTcs.TrySetResult(c.Response.StatusCode);
                });

            using (var server = new TestServer(handler, new TestServiceContext(), new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)),
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
                        await connection.ReceiveForcedEnd(
                            "HTTP/1.1 400 Bad Request",
                            "Connection: close",
                            $"Date: {server.Context.DateHeaderValue}",
                            "Content-Length: 0",
                            "",
                            "");
                    }
                }

                var disposedStatusCode = await disposedTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                Assert.Equal(expectedServerStatusCode, (HttpStatusCode)disposedStatusCode);
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/pull/1111/files#r80584475 explains the reason for this test.
        [Fact]
        public async Task SingleErrorResponseSentWhenAppSwallowsBadRequestException()
        {
            BadHttpRequestException readException = null;

            using (var server = new TestServer(async httpContext =>
            {
                readException = await Assert.ThrowsAsync<BadHttpRequestException>(
                    async () => await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1));
            }))
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
                        "HTTP/1.1 400 Bad Request",
                        "Connection: close",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.NotNull(readException);
        }

        [Fact]
        public async Task TransferEncodingChunkedSetOnUnknownLengthHttp11Response()
        {
            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync("hello, ");
                await httpContext.Response.WriteAsync("world");
            }))
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
            }))
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
            }))
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

            var logTcs = new TaskCompletionSource<object>();
            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.ConnectionHeadResponseBodyWrite(It.IsAny<string>(), response.Length))
                .Callback<string, long>((connectionId, count) => logTcs.SetResult(null));

            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync(response);
                await httpContext.Response.Body.FlushAsync();
            }, new TestServiceContext { Log = mockKestrelTrace.Object }))
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
                    await logTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                }
            }

            mockKestrelTrace.Verify(kestrelTrace =>
                kestrelTrace.ConnectionHeadResponseBodyWrite(It.IsAny<string>(), response.Length), Times.Once);
        }

        [Fact]
        public async Task ThrowsAndClosesConnectionWhenAppWritesMoreThanContentLengthWrite()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext
            {
                Log = new TestKestrelTrace(testLogger),
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

                    await connection.WaitForConnectionClose().TimeoutAfter(TimeSpan.FromSeconds(30));
                }
            }

            var logMessage = Assert.Single(testLogger.Messages, message => message.LogLevel == LogLevel.Error);

            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 11).",
                logMessage.Exception.Message);

        }

        [Fact]
        public async Task ThrowsAndClosesConnectionWhenAppWritesMoreThanContentLengthWriteAsync()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext { Log = new TestKestrelTrace(testLogger) };

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

            var logMessage = Assert.Single(testLogger.Messages, message => message.LogLevel == LogLevel.Error);
            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 11).",
                logMessage.Exception.Message);
        }

        [Fact]
        public async Task InternalServerErrorAndConnectionClosedOnWriteWithMoreThanContentLengthAndResponseNotStarted()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext
            {
                Log = new TestKestrelTrace(testLogger),
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

            var logMessage = Assert.Single(testLogger.Messages, message => message.LogLevel == LogLevel.Error);
            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 5).",
                logMessage.Exception.Message);
        }

        [Fact]
        public async Task InternalServerErrorAndConnectionClosedOnWriteAsyncWithMoreThanContentLengthAndResponseNotStarted()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext { Log = new TestKestrelTrace(testLogger) };

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

            var logMessage = Assert.Single(testLogger.Messages, message => message.LogLevel == LogLevel.Error);
            Assert.Equal(
                $"Response Content-Length mismatch: too many bytes written (12 of 5).",
                logMessage.Exception.Message);
        }

        [Fact]
        public async Task WhenAppWritesLessThanContentLengthErrorLogged()
        {
            var logTcs = new TaskCompletionSource<object>();
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
            }, new TestServiceContext { Log = mockTrace.Object }))
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
                    await logTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

                    // The server should close the connection in this situation.
                    await connection.WaitForConnectionClose().TimeoutAfter(TimeSpan.FromSeconds(10));
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
                Assert.True(await requestAborted.WaitAsync(TimeSpan.FromSeconds(10)));
            }, new TestServiceContext { Log = mockTrace.Object }))
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
                Assert.True(await requestAborted.WaitAsync(TimeSpan.FromSeconds(10)));
            }

            // With the server disposed we know all connections were drained and all messages were logged.
            mockTrace.Verify(trace => trace.ApplicationError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InvalidOperationException>()), Times.Never);
        }

        [Fact]
        public async Task WhenAppSetsContentLengthButDoesNotWriteBody500ResponseSentAndConnectionDoesNotClose()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext { Log = new TestKestrelTrace(testLogger) };

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

            var error = testLogger.Messages.Where(message => message.LogLevel == LogLevel.Error);
            Assert.Equal(2, error.Count());
            Assert.All(error, message => message.Equals("Response Content-Length mismatch: too few bytes written (0 of 5)."));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task WhenAppSetsContentLengthToZeroAndDoesNotWriteNoErrorIsThrown(bool flushResponse)
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext { Log = new TestKestrelTrace(testLogger) };

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

            Assert.Equal(0, testLogger.ApplicationErrorsLogged);
        }

        // https://tools.ietf.org/html/rfc7230#section-3.3.3
        // If a message is received with both a Transfer-Encoding and a
        // Content-Length header field, the Transfer-Encoding overrides the
        // Content-Length.
        [Fact]
        public async Task WhenAppSetsTransferEncodingAndContentLengthWritingLessIsNotAnError()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext { Log = new TestKestrelTrace(testLogger) };

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

            Assert.Equal(0, testLogger.ApplicationErrorsLogged);
        }

        // https://tools.ietf.org/html/rfc7230#section-3.3.3
        // If a message is received with both a Transfer-Encoding and a
        // Content-Length header field, the Transfer-Encoding overrides the
        // Content-Length.
        [Fact]
        public async Task WhenAppSetsTransferEncodingAndContentLengthWritingMoreIsNotAnError()
        {
            var testLogger = new TestApplicationErrorLogger();
            var serviceContext = new TestServiceContext { Log = new TestKestrelTrace(testLogger) };

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

            Assert.Equal(0, testLogger.ApplicationErrorsLogged);
        }

        [Fact]
        public async Task HeadResponseCanContainContentLengthHeader()
        {
            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.ContentLength = 42;
                return Task.CompletedTask;
            }))
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
            }))
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
            var serviceContext = new TestServiceContext { ServerOptions = { AllowSynchronousIO = true } };

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                httpContext.Response.Body.Write(Encoding.ASCII.GetBytes("hello, world"), 0, 12);
                flushed.Wait();
                return Task.CompletedTask;
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
                flushed.Wait();
                await httpContext.Response.WriteAsync("hello, world");
            }))
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
            var connectionClosed = new ManualResetEventSlim();
            var requestStarted = new ManualResetEventSlim();
            var tcs = new TaskCompletionSource<object>();

            using (var server = new TestServer(async httpContext =>
            {
                try
                {
                    requestStarted.Set();
                    connectionClosed.Wait();
                    httpContext.Response.ContentLength = 12;
                    await httpContext.Response.WriteAsync("hello, world");
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");

                    requestStarted.Wait();
                    connection.Shutdown(SocketShutdown.Send);
                    await connection.WaitForConnectionClose().TimeoutAfter(TimeSpan.FromSeconds(30));
                }

                connectionClosed.Set();

                await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(30));
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
            }))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "gg");
                    await responseWritten.WaitAsync().TimeoutAfter(TimeSpan.FromSeconds(30));
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
            }))
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
            }))
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
            }))
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
            var serviceContext = new TestServiceContext { ServerOptions = { AllowSynchronousIO = true } };

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
            var serviceContext = new TestServiceContext { ServerOptions = { AllowSynchronousIO = true } };

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
            }))
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
            }))
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
            }))
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
        public async Task WhenResponseNotStartedResponseEndedAfterConsumingRequestBody()
        {
            using (var server = new TestServer(httpContext => Task.CompletedTask))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Transfer-Encoding: chunked",
                        "",
                        "gg");

                    // If the expected behavior is regressed, this will receive
                    // a success response because the server flushed the response
                    // before reading the malformed chunk header in the request.
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

        [Fact]
        public async Task Sending100ContinueDoesNotStartResponse()
        {
            using (var server = new TestServer(httpContext =>
            {
                return httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
            }))
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

                    // This will be consumed by Http1Connection when it attempts to
                    // consume the request body and will cause an error.
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
        }

        [Fact]
        public async Task Sending100ContinueAndResponseSendsChunkTerminatorBeforeConsumingRequestBody()
        {
            using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
                await httpContext.Response.WriteAsync("hello, world");
            }))
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
            var serviceContext = new TestServiceContext();

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
            var testContext = new TestServiceContext();

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
            var testContext = new TestServiceContext();

            using (var server = new TestServer(async httpContext =>
            {
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).TimeoutAfter(TimeSpan.FromSeconds(10)));
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
            var testContext = new TestServiceContext();

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
            var testContext = new TestServiceContext();

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
            var testContext = new TestServiceContext();

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
            var testContext = new TestServiceContext();

            bool onStartingCalled = false;

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

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
            Assert.Equal(2, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingInOnStartingResultsInFailedWritesAnd500Response(ListenOptions listenOptions)
        {
            var callback1Called = false;
            var callback2CallCount = 0;

            var testContext = new TestServiceContext();
            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

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
            Assert.Equal(2, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingInOnCompletedIsLoggedAndClosesConnection(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

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

            // All OnCompleted callbacks should be called even if they throw.
            Assert.Equal(2, testLogger.ApplicationErrorsLogged);
            Assert.True(onCompletedCalled1);
            Assert.True(onCompletedCalled2);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingAfterWritingKillsConnection(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

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
            Assert.Equal(1, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowingAfterPartialWriteKillsConnection(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

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
            Assert.Equal(1, testLogger.ApplicationErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task ThrowsOnWriteWithRequestAbortedTokenAfterRequestIsAborted(ListenOptions listenOptions)
        {
            // This should match _maxBytesPreCompleted in SocketOutput
            var maxBytesPreCompleted = 65536;

            // Ensure string is long enough to disable write-behind buffering
            var largeString = new string('a', maxBytesPreCompleted + 1);

            var writeTcs = new TaskCompletionSource<object>();
            var requestAbortedWh = new ManualResetEventSlim();
            var requestStartWh = new ManualResetEventSlim();

            using (var server = new TestServer(async httpContext =>
            {
                requestStartWh.Set();

                var response = httpContext.Response;
                var request = httpContext.Request;
                var lifetime = httpContext.Features.Get<IHttpRequestLifetimeFeature>();

                lifetime.RequestAborted.Register(() => requestAbortedWh.Set());
                Assert.True(requestAbortedWh.Wait(TimeSpan.FromSeconds(10)));

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
            }, new TestServiceContext(), listenOptions))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 0",
                        "",
                        "");

                    Assert.True(requestStartWh.Wait(TimeSpan.FromSeconds(10)));
                }

                // Write failed - can throw TaskCanceledException or OperationCanceledException,
                // dependending on how far the canceled write goes.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await writeTcs.Task).TimeoutAfter(TimeSpan.FromSeconds(15));

                // RequestAborted tripped
                Assert.True(requestAbortedWh.Wait(TimeSpan.FromSeconds(1)));
            }
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task NoErrorsLoggedWhenServerEndsConnectionBeforeClient(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

            var testLogger = new TestApplicationErrorLogger();
            testContext.Log = new KestrelTrace(testLogger);

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

            Assert.Equal(0, testLogger.TotalErrorsLogged);
        }

        [Theory]
        [MemberData(nameof(ConnectionAdapterData))]
        public async Task NoResponseSentWhenConnectionIsClosedByServerBeforeClientFinishesSendingRequest(ListenOptions listenOptions)
        {
            var testContext = new TestServiceContext();

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
            var testContext = new TestServiceContext();

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

            var testContext = new TestServiceContext();

            var callOrder = new Stack<int>();
            var onStartingTcs = new TaskCompletionSource<object>();

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
                    await onStartingTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
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

            var testContext = new TestServiceContext();

            var callOrder = new Stack<int>();
            var onCompletedTcs = new TaskCompletionSource<object>();

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
                    await onCompletedTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
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
            }))
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
            var testContext = new TestServiceContext
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

        [Fact]
        public void ConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger($"{typeof(ResponseTests).FullName}.{nameof(ConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate)}");

                var chunkSize = 64 * 1024;
                var chunks = 128;
                var responseSize = chunks * chunkSize;

                var requestAborted = new ManualResetEventSlim();
                var messageLogged = new ManualResetEventSlim();
                var mockKestrelTrace = new Mock<KestrelTrace>(loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel")) { CallBase = true };
                mockKestrelTrace
                    .Setup(trace => trace.ResponseMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback(() => messageLogged.Set());

                var testContext = new TestServiceContext
                {
                    LoggerFactory = loggerFactory,
                    Log = mockKestrelTrace.Object,
                    SystemClock = new SystemClock(),
                    ServerOptions =
                    {
                        Limits =
                        {
                            MinResponseDataRate = new MinDataRate(bytesPerSecond: double.MaxValue, gracePeriod: TimeSpan.FromSeconds(2))
                        }
                    }
                };

                var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
                listenOptions.ConnectionAdapters.Add(new LoggingConnectionAdapter(loggerFactory.CreateLogger<LoggingConnectionAdapter>()));

                var appLogger = loggerFactory.CreateLogger("App");
                async Task App(HttpContext context)
                {
                    appLogger.LogInformation("Request received");
                    context.RequestAborted.Register(() => requestAborted.Set());

                    context.Response.ContentLength = responseSize;

                    for (var i = 0; i < chunks; i++)
                    {
                        await context.Response.WriteAsync(new string('a', chunkSize), context.RequestAborted);
                        appLogger.LogInformation("Wrote chunk of {chunkSize} bytes", chunkSize);
                    }
                }

                using (var server = new TestServer(App, testContext, listenOptions))
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.ReceiveBufferSize = 1;

                        socket.Connect(new IPEndPoint(IPAddress.Loopback, server.Port));
                        logger.LogInformation("Sending request");
                        socket.Send(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: \r\n\r\n"));
                        logger.LogInformation("Sent request");

                        var sw = Stopwatch.StartNew();
                        logger.LogInformation("Waiting for connection to abort.");
                        Assert.True(messageLogged.Wait(TimeSpan.FromSeconds(120)), "The expected message was not logged within the timeout period.");
                        Assert.True(requestAborted.Wait(TimeSpan.FromSeconds(120)), "The request was not aborted within the timeout period.");
                        sw.Stop();
                        logger.LogInformation("Connection was aborted after {totalMilliseconds}ms.", sw.ElapsedMilliseconds);

                        var totalReceived = 0;
                        var received = 0;

                        try
                        {
                            var buffer = new byte[chunkSize];

                            do
                            {
                                received = socket.Receive(buffer);
                                totalReceived += received;
                            } while (received > 0 && totalReceived < responseSize);
                        }
                        catch (SocketException) { }
                        catch (IOException)
                        {
                            // Socket.Receive could throw, and that is fine
                        }

                        // Since we expect writes to be cut off by the rate control, we should never see the entire response
                        logger.LogInformation("Received {totalReceived} bytes", totalReceived);
                        Assert.NotEqual(responseSize, totalReceived);
                    }
                }
            }
        }

        [Fact]
        public async Task HttpsConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate()
        {
            const int chunkSize = 64 * 1024;
            const int chunks = 128;

            var certificate = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");

            var messageLogged = new ManualResetEventSlim();
            var aborted = new ManualResetEventSlim();

            var mockKestrelTrace = new Mock<IKestrelTrace>();
            mockKestrelTrace
                .Setup(trace => trace.ResponseMininumDataRateNotSatisfied(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(() => messageLogged.Set());

            var testContext = new TestServiceContext
            {
                Log = mockKestrelTrace.Object,
                SystemClock = new SystemClock(),
                ServerOptions =
                {
                    Limits =
                    {
                        MinResponseDataRate = new MinDataRate(bytesPerSecond: double.MaxValue, gracePeriod: TimeSpan.FromSeconds(2))
                    }
                }
            };

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
                    aborted.Set();
                });

                context.Response.ContentLength = chunks * chunkSize;

                for (var i = 0; i < chunks; i++)
                {
                    await context.Response.WriteAsync(new string('a', chunkSize), context.RequestAborted);
                }
            }, testContext, listenOptions))
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, server.Port);

                    using (var sslStream = new SslStream(client.GetStream(), false, (sender, cert, chain, errors) => true, null))
                    {
                        await sslStream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false);

                        var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                        await sslStream.WriteAsync(request, 0, request.Length);

                        Assert.True(aborted.Wait(TimeSpan.FromSeconds(60)));

                        using (var reader = new StreamReader(sslStream, encoding: Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false))
                        {
                            await reader.ReadToEndAsync().TimeoutAfter(TimeSpan.FromSeconds(30));
                        }

                        Assert.True(messageLogged.Wait(TimeSpan.FromSeconds(10)));
                    }
                }
            }
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
