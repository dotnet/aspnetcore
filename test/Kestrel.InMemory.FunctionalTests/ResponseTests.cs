// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ResponseTests : TestApplicationErrorLoggerLoggedTest
    {
        [Fact]
        public async Task OnCompleteCalledEvenWhenOnStartingNotCalled()
        {
            var onStartingCalled = false;
            TaskCompletionSource<object> onCompletedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(context =>
            {
                context.Response.OnStarting(() => Task.Run(() => onStartingCalled = true));
                context.Response.OnCompleted(() => Task.Run(() =>
                {
                    onCompletedTcs.SetResult(null);
                }));

                // Prevent OnStarting call (see HttpProtocol.ProcessRequestsAsync()).
                throw new Exception();
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
                        $"HTTP/1.1 500 Internal Server Error",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");

                    await onCompletedTcs.Task.DefaultTimeout();
                    Assert.False(onStartingCalled);
                }
            }
        }

        [Fact]
        public async Task OnStartingThrowsWhenSetAfterResponseHasAlreadyStarted()
        {
            InvalidOperationException ex = null;

            using (var server = new TestServer(async context =>
            {
                await context.Response.WriteAsync("hello, world");
                await context.Response.Body.FlushAsync();
                ex = Assert.Throws<InvalidOperationException>(() => context.Response.OnStarting(_ => Task.CompletedTask, null));
            }, new TestServiceContext(LoggerFactory)))
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
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");

                    Assert.NotNull(ex);
                }
            }
        }

        [Fact]
        public async Task ResponseBodyWriteAsyncCanBeCancelled()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);
            serviceContext.ServerOptions.Limits.MaxResponseBufferSize = 5;
            var cts = new CancellationTokenSource();
            var appTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                try
                {
                    await context.Response.WriteAsync("hello", cts.Token).DefaultTimeout();

                    var task = context.Response.WriteAsync("world", cts.Token);
                    Assert.False(task.IsCompleted);

                    writeStartedTcs.TrySetResult(null);

                    await task.DefaultTimeout();
                }
                catch (Exception ex)
                {
                    appTcs.TrySetException(ex);
                }
                finally
                {
                    appTcs.TrySetResult(null);
                    writeStartedTcs.TrySetCanceled();
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

                    await writeStartedTcs.Task.DefaultTimeout();

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
            using (var server = new TestServer(async context =>
            {
                context.Response.OnCompleted(_ => throw new Exception(), null);
                await context.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
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
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task OnCompletedShouldNotBlockAResponse()
        {
            var delayTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async context =>
            {
                context.Response.OnCompleted(async () =>
                {
                    await delayTcs.Task;
                });
                await context.Response.WriteAsync("hello, world");
            }, new TestServiceContext(LoggerFactory)))
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
                        "c",
                        "hello, world",
                        "0",
                        "",
                        "");
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

                    await connection.ReceiveEnd(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            await onCompletedTcs.Task.DefaultTimeout();
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
                    await connection.ReceiveEnd(
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

                    await connection.WaitForConnectionClose();
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
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
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
                    await connection.WaitForConnectionClose();
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
            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockTrace = new Mock<IKestrelTrace>();

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.RequestAborted.Register(() =>
                {
                    requestAborted.SetResult(null);
                });

                httpContext.Response.ContentLength = 12;
                await httpContext.Response.WriteAsync("hello,");

                // Wait until the request is aborted so we know HttpProtocol will skip the response content length check.
                await requestAborted.Task.DefaultTimeout();
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
                await requestAborted.Task.DefaultTimeout();
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
            var flushed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                await httpContext.Response.WriteAsync("hello, world");
                await flushed.Task;
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

                    flushed.SetResult(null);
                }
            }
        }

        [Fact]
        public async Task HeadResponseBodyNotWrittenWithSyncWrite()
        {
            var flushed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var serviceContext = new TestServiceContext(LoggerFactory) { ServerOptions = { AllowSynchronousIO = true } };

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                httpContext.Response.Body.Write(Encoding.ASCII.GetBytes("hello, world"), 0, 12);
                await flushed.Task;
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

                    flushed.SetResult(null);
                }
            }
        }

        [Fact]
        public async Task ZeroLengthWritesFlushHeaders()
        {
            var flushed = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                await httpContext.Response.WriteAsync("");
                await flushed.Task;
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

                    flushed.SetResult(null);

                    await connection.Receive("hello, world");
                }
            }
        }

        [Fact]
        public async Task AppCanWriteOwnBadRequestResponse()
        {
            var expectedResponse = string.Empty;
            var responseWritten = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    responseWritten.SetResult(null);
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
                    await responseWritten.Task.DefaultTimeout();
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
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
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
                    await connection.Receive(
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
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
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

        [Fact]
        public async Task Http11ResponseSentToHttp10Request()
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EchoApp, serviceContext))
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

        [Fact]
        public async Task ZeroContentLengthSetAutomaticallyAfterNoWrites()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EmptyApp, testContext))
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
                    await connection.Receive(
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

        [Fact]
        public async Task ZeroContentLengthSetAutomaticallyForNonKeepAliveRequests()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(async httpContext =>
            {
                Assert.Equal(0, await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
            }, testContext))
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

        [Fact]
        public async Task ZeroContentLengthNotSetAutomaticallyForHeadRequests()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(TestApp.EmptyApp, testContext))
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
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
                    await connection.Receive(
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

        [Fact]
        public async Task ConnectionClosedAfter101Response()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
                        "Host:",
                        "Connection: Upgrade",
                        "",
                        "");
                    await connection.ReceiveEnd(
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
                    await connection.ReceiveEnd(
                        "HTTP/1.1 101 Switching Protocols",
                        "Connection: Upgrade",
                        $"Date: {testContext.DateHeaderValue}",
                        "",
                        "hello, world");
                }
            }
        }

        [Fact]
        public async Task ThrowingResultsIn500Response()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
            }, testContext))
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
                    await connection.ReceiveEnd(
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


        [Fact]
        public async Task ThrowingInOnStartingResultsInFailedWritesAnd500Response()
        {
            var callback1Called = false;
            var callback2CallCount = 0;

            var testContext = new TestServiceContext(LoggerFactory);

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
            }, testContext))
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

        [Fact]
        public async Task ThrowingInOnCompletedIsLogged()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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

        [Fact]
        public async Task ThrowingAfterWritingKillsConnection()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
            }, testContext))
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

            Assert.True(onStartingCalled);
            Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
        }

        [Fact]
        public async Task ThrowingAfterPartialWriteKillsConnection()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
            }, testContext))
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
                        "Hello");
                }
            }

            Assert.True(onStartingCalled);
            Assert.Single(TestApplicationErrorLogger.Messages, message => message.LogLevel == LogLevel.Error);
        }


        [Fact]
        public async Task NoErrorsLoggedWhenServerEndsConnectionBeforeClient()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
                    await connection.ReceiveEnd(
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

        [Fact]
        public async Task NoResponseSentWhenConnectionIsClosedByServerBeforeClientFinishesSendingRequest()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            using (var server = new TestServer(httpContext =>
            {
                httpContext.Abort();
                return Task.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 1",
                        "",
                        "");
                    await connection.ReceiveEnd();
                }
            }
        }

        [Fact]
        public async Task ResponseHeadersAreResetOnEachRequest()
        {
            var testContext = new TestServiceContext(LoggerFactory);

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
            }, testContext))
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

        [Fact]
        public async Task OnStartingCallbacksAreCalledInLastInFirstOutOrder()
        {
            const string response = "hello, world";

            var testContext = new TestServiceContext(LoggerFactory);

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

        [Fact]
        public async Task OnCompletedCallbacksAreCalledInLastInFirstOutOrder()
        {
            const string response = "hello, world";

            var testContext = new TestServiceContext(LoggerFactory);

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
                options => options.ListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))),
                services => services.AddSingleton(mockHttpContextFactory.Object)))
            {
                using (var connection = server.CreateConnection())
                {
                    if (!sendMalformedRequest)
                    {
                        await connection.Send(
                            "GET / HTTP/1.1",
                            "Host:",
                            "Connection: close",
                            "",
                            "");

                        using (var reader = new StreamReader(connection.Stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                        {
                            try
                            {
                                var response = await reader.ReadToEndAsync().DefaultTimeout();
                                Assert.Equal(expectedClientStatusCode, GetStatus(response));
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
                        await connection.Send(
                            "POST / HTTP/1.1",
                            "Host:",
                            "Transfer-Encoding: chunked",
                            "",
                            "gg");

                        if (expectedClientStatusCode == HttpStatusCode.OK)
                        {
                            await connection.ReceiveEnd(
                                "HTTP/1.1 200 OK",
                                $"Date: {server.Context.DateHeaderValue}",
                                "Content-Length: 0",
                                "",
                                "");
                        }
                        else
                        {
                            await connection.ReceiveEnd(
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

        private static HttpStatusCode GetStatus(string response)
        {
            var statusStart = response.IndexOf(' ') + 1;
            var statusEnd = response.IndexOf(' ', statusStart) - 1;
            var statusLength = statusEnd - statusStart + 1;

            if (statusLength < 1)
            {
                throw new InvalidDataException($"No StatusCode found in '{response}'");
            }

            return (HttpStatusCode)int.Parse(response.Substring(statusStart, statusLength));
        }
    }
}
