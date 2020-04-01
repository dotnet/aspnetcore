// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
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

            await using (var server = new TestServer(context =>
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

            await using (var server = new TestServer(async context =>
            {
                await context.Response.WriteAsync("hello, world");
                await context.Response.BodyWriter.FlushAsync();
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
        public async Task OnStartingThrowsWhenSetAfterStartAsyncIsCalled()
        {
            InvalidOperationException ex = null;

            await using (var server = new TestServer(async context =>
            {
                await context.Response.StartAsync();
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
            var cts = new CancellationTokenSource();
            var appTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeBlockedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async context =>
            {
                try
                {
                    await context.Response.WriteAsync("hello", cts.Token).DefaultTimeout();

                    var data = new byte[1024 * 1024 * 10];

                    var timerTask = Task.Delay(TimeSpan.FromSeconds(1));
                    var writeTask = context.Response.BodyWriter.WriteAsync(new Memory<byte>(data, 0, data.Length), cts.Token).AsTask().DefaultTimeout();
                    var completedTask = await Task.WhenAny(writeTask, timerTask);

                    while (completedTask == writeTask)
                    {
                        await writeTask;
                        timerTask = Task.Delay(TimeSpan.FromSeconds(1));
                        writeTask = context.Response.BodyWriter.WriteAsync(new Memory<byte>(data, 0, data.Length), cts.Token).AsTask().DefaultTimeout();
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
            await using (var server = new TestServer(async context =>
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

            await using (var server = new TestServer(async context =>
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

            await using (var server = new TestServer(httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
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

            Assert.Contains(TestSink.Writes, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task TransferEncodingChunkedSetOnUnknownLengthHttp11Response()
        {
            await using (var server = new TestServer(async httpContext =>
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
        [InlineData(StatusCodes.Status304NotModified)]
        public async Task TransferEncodingChunkedNotSetOnNonBodyResponse(int statusCode)
        {
            await using (var server = new TestServer(httpContext =>
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
        public async Task ContentLengthZeroSetOn205Response()
        {
            await using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.StatusCode = 205;
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
                        $"HTTP/1.1 205 Reset Content",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Theory]
        [InlineData(StatusCodes.Status204NoContent)]
        [InlineData(StatusCodes.Status304NotModified)]
        public async Task AttemptingToWriteFailsForNonBodyResponse(int statusCode)
        {
            var responseWriteTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.StatusCode = statusCode;

                try
                {
                    await httpContext.Response.WriteAsync("hello, world");
                }
                catch (Exception ex)
                {
                    responseWriteTcs.TrySetException(ex);
                    throw;
                }

                responseWriteTcs.TrySetResult("This should not be reached.");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");


                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => responseWriteTcs.Task).DefaultTimeout();
                    Assert.Equal(CoreStrings.FormatWritingToResponseBodyNotSupported(statusCode), ex.Message);

                    await connection.Receive(
                        $"HTTP/1.1 {Encoding.ASCII.GetString(ReasonPhrases.ToStatusBytes(statusCode))}",
                        $"Date: {server.Context.DateHeaderValue}",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task AttemptingToWriteFailsFor205Response()
        {
            var responseWriteTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.StatusCode = 205;

                try
                {
                    await httpContext.Response.WriteAsync("hello, world");
                }
                catch (Exception ex)
                {
                    responseWriteTcs.TrySetException(ex);
                    throw;
                }

                responseWriteTcs.TrySetResult("This should not be reached.");
            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");


                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => responseWriteTcs.Task).DefaultTimeout();
                    Assert.Equal(CoreStrings.FormatWritingToResponseBodyNotSupported(205), ex.Message);

                    await connection.Receive(
                        $"HTTP/1.1 205 Reset Content",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task TransferEncodingNotSetOnHeadResponse()
        {
            await using (var server = new TestServer(httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.WriteAsync(response);
                await httpContext.Response.BodyWriter.FlushAsync();
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

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("hello,"), 0, 6));
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes(" world"), 0, 6));
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

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
            {
                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = 5;
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, 0, response.Length));
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

            await using (var server = new TestServer(async httpContext =>
            {
                var response = Encoding.ASCII.GetBytes("hello, world");
                httpContext.Response.ContentLength = 5;
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, 0, response.Length));
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

            await using (var server = new TestServer(async httpContext =>
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
        public async Task WhenAppWritesLessThanContentLengthCompleteThrowsAndErrorLogged()
        {
            InvalidOperationException completeEx = null;

            var logTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockTrace = new Mock<IKestrelTrace>();
            mockTrace
                .Setup(trace => trace.ApplicationError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<InvalidOperationException>()))
                .Callback<string, string, Exception>((connectionId, requestId, ex) =>
                {
                    logTcs.SetResult(null);
                });

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 13;
                await httpContext.Response.WriteAsync("hello, world");

                completeEx = Assert.Throws<InvalidOperationException>(() => httpContext.Response.BodyWriter.Complete());

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

            Assert.NotNull(completeEx);
        }

        [Fact]
        public async Task WhenAppWritesLessThanContentLengthButRequestIsAbortedErrorNotLogged()
        {
            var requestAborted = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var mockTrace = new Mock<IKestrelTrace>();

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(httpContext =>
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
        [InlineData(true)]
        [InlineData(false)]
        public async Task WhenAppSetsContentLengthToZeroAndDoesNotWriteNoErrorIsThrown(bool flushResponse)
        {
            var serviceContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 0;

                if (flushResponse)
                {
                    await httpContext.Response.BodyWriter.FlushAsync();
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

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
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
            await using (var server = new TestServer(httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("hello, world"), 0, 12));
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

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
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
            await using (var server = new TestServer(async httpContext =>
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
            await using (var server = new TestServer(async httpContext =>
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
            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
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
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, 0, response.Length));
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
        public async Task FirstWriteVerifiedAfterOnStartingWithResponseBody()
        {
            var serviceContext = new TestServiceContext(LoggerFactory) { ServerOptions = { AllowSynchronousIO = true } };

            await using (var server = new TestServer(async httpContext =>
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
                await httpContext.Response.Body.WriteAsync(new Memory<byte>(response, 0, response.Length));
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

            await using (var server = new TestServer(async httpContext =>
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
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, 0, response.Length / 2));
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, response.Length / 2, response.Length - response.Length / 2));
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
        public async Task SubsequentWriteVerifiedAfterOnStartingWithResponseBody()
        {
            var serviceContext = new TestServiceContext(LoggerFactory) { ServerOptions = { AllowSynchronousIO = true } };

            await using (var server = new TestServer(async httpContext =>
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
                await httpContext.Response.Body.WriteAsync(new Memory<byte>(response, 0, response.Length / 2));
                await httpContext.Response.Body.WriteAsync(new Memory<byte>(response, response.Length / 2, response.Length - response.Length / 2));
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
            await using (var server = new TestServer(httpContext =>
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
                return httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, 0, response.Length)).AsTask();
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
            await using (var server = new TestServer(async httpContext =>
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
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, 0, response.Length / 2));
                await httpContext.Response.BodyWriter.WriteAsync(new Memory<byte>(response, response.Length / 2, response.Length - response.Length / 2));
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
            await using (var server = new TestServer(async httpContext =>
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
            await using (var server = new TestServer(httpContext => Task.CompletedTask,
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

            Assert.Contains(TestApplicationErrorLogger.Messages, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task RequestDrainingFor100ContinueDoesNotBlockResponse()
        {
            var foundMessage = false;
            await using (var server = new TestServer(httpContext =>
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
                            if (message.EventId.Id == 17 && message.LogLevel <= LogLevel.Debug && message.Exception is BadHttpRequestException
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
            await using (var server = new TestServer(httpContext =>
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

            Assert.Contains(TestApplicationErrorLogger.Messages, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task Sending100ContinueAndResponseSendsChunkTerminatorBeforeConsumingRequestBody()
        {
            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(TestApp.EchoApp, serviceContext))
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

            await using (var server = new TestServer(TestApp.EmptyApp, testContext))
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

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(TestApp.EmptyApp, testContext))
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

            await using (var server = new TestServer(async httpContext =>
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
                        "304POST / HTTP/1.1",
                        "Host:",
                        "Content-Length: 3",
                        "",
                        "200");
                    await connection.Receive(
                        "HTTP/1.1 204 No Content",
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

            await using (var server = new TestServer(async httpContext =>
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

            await using (var server = new TestServer(httpContext =>
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

            await using (var server = new TestServer(async httpContext =>
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

                var writeException = await Assert.ThrowsAsync<ObjectDisposedException>(async () => await response.BodyWriter.FlushAsync());
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
        public async Task OnStartingThrowsInsideOnStartingCallbacksRuns()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(state1 =>
                {
                    response.OnStarting(state2 =>
                    {
                        tcs.TrySetResult(null);
                        return Task.CompletedTask;
                    },
                    null);

                    return Task.CompletedTask;

                }, null);

                response.Headers["Content-Length"] = new[] { "11" };

                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));
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
                        "");

                    await tcs.Task.DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task OnCompletedThrowsInsideOnCompletedCallbackRuns()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnCompleted(state1 =>
                {
                    response.OnCompleted(state2 =>
                    {
                        tcs.TrySetResult(null);

                        return Task.CompletedTask;
                    },
                    null);

                    return Task.CompletedTask;

                }, null);

                response.Headers["Content-Length"] = new[] { "11" };

                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));
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
                        "");

                    await tcs.Task.DefaultTimeout();
                }
            }
        }

        [Fact]
        public async Task ThrowingInOnCompletedIsLogged()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var onCompletedCalled1 = false;
            var onCompletedCalled2 = false;

            await using (var server = new TestServer(async httpContext =>
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

                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));
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

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                response.Headers["Content-Length"] = new[] { "11" };
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));
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

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.OnStarting(_ =>
                {
                    onStartingCalled = true;
                    return Task.FromResult<object>(null);
                }, null);

                response.Headers["Content-Length"] = new[] { "11" };
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello"), 0, 5));
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

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.Headers["Content-Length"] = new[] { "11" };
                await response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello World"), 0, 11));
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

            await using (var server = new TestServer(httpContext =>
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
        public async Task AppAbortIsLogged()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(httpContext =>
            {
                httpContext.Abort();
                return Task.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd();
                }
            }

            Assert.Single(TestApplicationErrorLogger.Messages.Where(m => m.Message.Contains(CoreStrings.ConnectionAbortedByApplication)));
        }

        [Fact]
        [QuarantinedTest]
        public async Task AppAbortViaIConnectionLifetimeFeatureIsLogged()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(httpContext =>
            {
                var feature = httpContext.Features.Get<IConnectionLifetimeFeature>();
                feature.Abort();

                // Ensure the response doesn't get flush before the abort is observed.
                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                feature.ConnectionClosed.Register(() => tcs.TrySetResult(null));

                return tcs.Task;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.ReceiveEnd();
                }
            }

            Assert.Single(TestApplicationErrorLogger.Messages.Where(m => m.Message.Contains("The connection was aborted by the application via IConnectionLifetimeFeature.Abort().")));
        }

        [Fact]
        public async Task ResponseHeadersAreResetOnEachRequest()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            IHeaderDictionary originalResponseHeaders = null;
            var firstRequest = true;

            await using (var server = new TestServer(httpContext =>
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
        public async Task StartAsyncDefaultToChunkedResponse()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
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
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncWithContentLengthAndEmptyWriteStillCallsFinalFlush()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 0;
                await httpContext.Response.StartAsync();
                await httpContext.Response.WriteAsync("");
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
                        "Content-Length: 0",
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncAndEmptyWriteStillCallsFinalFlush()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                await httpContext.Response.WriteAsync("");
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
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncWithSingleChunkedWriteStillWritesSuffix()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                await httpContext.Response.WriteAsync("Hello World!");
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
                        "Transfer-Encoding: chunked",
                        "",
                        "c",
                        "Hello World!",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncWithoutFlushStartsResponse()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                Assert.True(httpContext.Response.HasStarted);
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
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncThrowExceptionThrowsConnectionAbortedException()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedException = new Exception();
            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                throw expectedException;
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
                        "Transfer-Encoding: chunked",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncWithContentLengthThrowExceptionThrowsConnectionAbortedException()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedException = new Exception();
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.StartAsync();
                throw expectedException;
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
                        "");
                }
            }
        }

        [Fact]
        public async Task StartAsyncWithoutFlushingDoesNotFlush()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                Assert.True(httpContext.Response.HasStarted);

                // Verify that the response isn't flushed by verifying the TCS isn't set
                var res = await Task.WhenAny(tcs.Task, Task.Delay(1000)) == tcs.Task;
                Assert.False(res);
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
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                    // If we reach this point before the app exits, this means the flush finished early.
                    tcs.SetResult(null);
                }
            }
        }

        [Fact]
        public async Task StartAsyncWithContentLengthWritingWorks()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Headers["Content-Length"] = new[] { "11" };
                await httpContext.Response.StartAsync();
                await httpContext.Response.WriteAsync("Hello World");
                Assert.True(httpContext.Response.HasStarted);
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
        }

        [Fact]
        public async Task LargeWriteWithContentLengthWritingWorks()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedLength = 100000;
            var expectedString = new string('a', expectedLength);
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = expectedLength;
                await httpContext.Response.WriteAsync(expectedString);
                Assert.True(httpContext.Response.HasStarted);
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
                        $"Content-Length: {expectedLength}",
                        "",
                        expectedString);
                }
            }
        }

        [Fact]
        public async Task UnflushedContentLengthResponseIsFlushedAutomatically()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var expectedLength = 100000;
            var expectedString = new string('a', expectedLength);

            void WriteStringWithoutFlushing(PipeWriter writer, string content)
            {
                var encoder = Encoding.ASCII.GetEncoder();
                var encodedLength = Encoding.ASCII.GetByteCount(expectedString);
                var source = expectedString.AsSpan();
                var completed = false;

                while (!completed)
                {
                    encoder.Convert(source, writer.GetSpan(), flush: source.Length == 0, out var charsUsed, out var bytesUsed, out completed);
                    writer.Advance(bytesUsed);
                    source = source.Slice(charsUsed);
                }
            }

            await using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.ContentLength = expectedLength;

                WriteStringWithoutFlushing(httpContext.Response.BodyWriter, expectedString);

                Assert.False(httpContext.Response.HasStarted);
                return Task.CompletedTask;
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
                        $"Content-Length: {expectedLength}",
                        "",
                        expectedString);
                }
            }
        }

        [Fact]
        public async Task StartAsyncAndFlushWorks()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                await httpContext.Response.BodyWriter.FlushAsync();
                Assert.True(httpContext.Response.HasStarted);
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
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
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

            await using (var server = new TestServer(async context =>
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

            await using (var server = new TestServer(async context =>
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
        public async Task SynchronousWritesDisallowedByDefault()
        {
            await using (var server = new TestServer(async context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.False(bodyControlFeature.AllowSynchronousIO);

                context.Response.ContentLength = 6;

                // Synchronous writes now throw.
                var ioEx = Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(Encoding.ASCII.GetBytes("What!?"), 0, 6));
                Assert.Equal(CoreStrings.SynchronousWritesDisallowed, ioEx.Message);
                await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("Hello1"), 0, 6);

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
                }
            }
        }

        [Fact]
        public async Task SynchronousWritesAllowedByOptIn()
        {
            await using (var server = new TestServer(context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.False(bodyControlFeature.AllowSynchronousIO);
                bodyControlFeature.AllowSynchronousIO = true;
                context.Response.ContentLength = 6;
                context.Response.Body.Write(Encoding.ASCII.GetBytes("Hello1"), 0, 6);
                return Task.CompletedTask;
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
                }
            }
        }

        [Fact]
        public async Task SynchronousWritesCanBeAllowedGlobally()
        {
            var testContext = new TestServiceContext(LoggerFactory)
            {
                ServerOptions = { AllowSynchronousIO = true }
            };

            await using (var server = new TestServer(context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.True(bodyControlFeature.AllowSynchronousIO);

                context.Response.ContentLength = 6;
                context.Response.Body.Write(Encoding.ASCII.GetBytes("Hello!"), 0, 6);
                return Task.CompletedTask;
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
        public async Task SynchronousWritesCanBeDisallowedGlobally()
        {
            var testContext = new TestServiceContext(LoggerFactory)
            {
                ServerOptions = { AllowSynchronousIO = false }
            };

            await using (var server = new TestServer(context =>
            {
                var bodyControlFeature = context.Features.Get<IHttpBodyControlFeature>();
                Assert.False(bodyControlFeature.AllowSynchronousIO);

                context.Response.ContentLength = 6;

                // Synchronous writes now throw.
                var ioEx = Assert.Throws<InvalidOperationException>(() => context.Response.Body.Write(Encoding.ASCII.GetBytes("What!?"), 0, 6));
                Assert.Equal(CoreStrings.SynchronousWritesDisallowed, ioEx.Message);

                return context.Response.BodyWriter.WriteAsync(new Memory<byte>(Encoding.ASCII.GetBytes("Hello!"), 0, 6)).AsTask();
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
            await using (var server = new TestServer(httpContext =>
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

        [Fact]
        public async Task AdvanceNegativeValueThrowsArgumentOutOfRangeException()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;

                await response.StartAsync();

                Assert.Throws<ArgumentOutOfRangeException>(() => response.BodyWriter.Advance(-1));
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task AdvanceNegativeValueThrowsArgumentOutOfRangeExceptionWithStart()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(httpContext =>
            {
                var response = httpContext.Response;

                Assert.Throws<ArgumentOutOfRangeException>(() => response.BodyWriter.Advance(-1));
                return Task.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task AdvanceWithTooLargeOfAValueThrowInvalidOperationException()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(httpContext =>
            {
                var response = httpContext.Response;

                Assert.Throws<InvalidOperationException>(() => response.BodyWriter.Advance(1));
                return Task.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Transfer-Encoding: chunked",
                        "",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ContentLengthWithoutStartAsyncWithGetSpanWorks()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(httpContext =>
            {
                var response = httpContext.Response;
                response.ContentLength = 12;

                var span = response.BodyWriter.GetSpan(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(span);
                response.BodyWriter.Advance(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(span.Slice(6));
                response.BodyWriter.Advance(6);
                return Task.CompletedTask;
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "Hello World!");
                }

                await server.StopAsync();
            }
        }

        [Fact]
        public async Task ContentLengthWithGetMemoryWorks()
        {
            var testContext = new TestServiceContext(LoggerFactory);

            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.ContentLength = 12;

                await response.StartAsync();

                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("Hello ");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);

                var secondPartOfResponse = Encoding.ASCII.GetBytes("World!");
                secondPartOfResponse.CopyTo(memory.Slice(6));
                response.BodyWriter.Advance(6);
            }, testContext))
            {
                using (var connection = server.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: ",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {testContext.DateHeaderValue}",
                        "Content-Length: 12",
                        "",
                        "Hello World!");
                }
            }
        }

        [Fact]
        public async Task ResponseBodyCanWrite()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.ContentLength = 12;
                await httpContext.Response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world"));
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
                        "hello, world");
                }
            }
        }

        [Fact]
        public async Task ResponseBodyAndResponsePipeWorks()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                var response = httpContext.Response;
                response.ContentLength = 54;
                await response.StartAsync();
                var memory = response.BodyWriter.GetMemory(4096);
                var fisrtPartOfResponse = Encoding.ASCII.GetBytes("hello,");
                fisrtPartOfResponse.CopyTo(memory);
                response.BodyWriter.Advance(6);
                var secondPartOfResponse = Encoding.ASCII.GetBytes(" world\r\n");
                secondPartOfResponse.CopyTo(memory.Slice(6));
                response.BodyWriter.Advance(8);

                await response.Body.WriteAsync(Encoding.ASCII.GetBytes("hello, world\r\n"));
                await response.BodyWriter.WriteAsync(Encoding.ASCII.GetBytes("hello, world\r\n"));
                await response.WriteAsync("hello, world");

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
                        "Content-Length: 54",
                        "",
                        "hello, world",
                        "hello, world",
                        "hello, world",
                        "hello, world");
                }
            }
        }

        [Fact]
        public async Task ResponseBodyWriterCompleteWithoutExceptionDoesNotThrow()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.BodyWriter.Complete();
                await Task.CompletedTask;
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
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseBodyWriterCompleteWithoutExceptionWritesDoesThrow()
        {
            InvalidOperationException writeEx = null;

            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.BodyWriter.Complete();
                writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => httpContext.Response.WriteAsync("test"));
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
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.NotNull(writeEx);
        }

        [Fact]
        public async Task ResponseAdvanceStateIsResetWithMultipleReqeusts()
        {
            var secondRequest = false;
            await using (var server = new TestServer(async httpContext =>
            {
                if (secondRequest)
                {
                    return;
                }

                var memory = httpContext.Response.BodyWriter.GetMemory();
                Encoding.ASCII.GetBytes("a").CopyTo(memory);
                httpContext.Response.BodyWriter.Advance(1);
                await httpContext.Response.BodyWriter.FlushAsync();
                secondRequest = true;

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
                        "1",
                        "a",
                        "0",
                        "",
                        "");

                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseStartCalledAndAutoChunkStateIsResetWithMultipleReqeusts()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                var memory = httpContext.Response.BodyWriter.GetMemory();
                Encoding.ASCII.GetBytes("a").CopyTo(memory);
                httpContext.Response.BodyWriter.Advance(1);
                await httpContext.Response.BodyWriter.FlushAsync();

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
                        "1",
                        "a",
                        "0",
                        "",
                        "");

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
                        "1",
                        "a",
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseStartCalledStateIsResetWithMultipleReqeusts()
        {
            var flip = false;
            await using (var server = new TestServer(async httpContext =>
            {
                if (flip)
                {
                    httpContext.Response.ContentLength = 1;
                    var memory = httpContext.Response.BodyWriter.GetMemory();
                    Encoding.ASCII.GetBytes("a").CopyTo(memory);
                    httpContext.Response.BodyWriter.Advance(1);
                    await httpContext.Response.BodyWriter.FlushAsync();
                }
                else
                {
                    var memory = httpContext.Response.BodyWriter.GetMemory();
                    Encoding.ASCII.GetBytes("a").CopyTo(memory);
                    httpContext.Response.BodyWriter.Advance(1);
                    await httpContext.Response.BodyWriter.FlushAsync();
                }
                flip = !flip;

            }, new TestServiceContext(LoggerFactory)))
            {
                using (var connection = server.CreateConnection())
                {
                    for (var i = 0; i < 3; i++)
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
                            "1",
                            "a",
                            "0",
                            "",
                            "");

                        await connection.Send(
                            "GET / HTTP/1.1",
                            "Host:",
                            "",
                            "");
                        await connection.Receive(
                            "HTTP/1.1 200 OK",
                            $"Date: {server.Context.DateHeaderValue}",
                            "Content-Length: 1",
                            "",
                            "a");
                    }
                }
            }
        }

        [Fact]
        public async Task ResponseIsLeasedMemoryInvalidStateIsResetWithMultipleReqeusts()
        {
            var secondRequest = false;
            await using (var server = new TestServer(httpContext =>
            {
                if (secondRequest)
                {
                    Assert.Throws<InvalidOperationException>(() => httpContext.Response.BodyWriter.Advance(1));
                    return Task.CompletedTask;
                }

                var memory = httpContext.Response.BodyWriter.GetMemory();
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
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");

                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host:",
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponsePipeWriterCompleteWithException()
        {
            var expectedException = new Exception();
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.BodyWriter.Complete(expectedException);
                await Task.CompletedTask;
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
                    Assert.Contains(TestSink.Writes, w => w.EventId.Id == 13 && w.LogLevel == LogLevel.Error
                        && w.Exception is ConnectionAbortedException && w.Exception.InnerException == expectedException);
                }
            }
        }

        [Fact]
        public async Task ResponseCompleteGetMemoryReturnsRentedMemory()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                await httpContext.Response.StartAsync();
                httpContext.Response.BodyWriter.Complete();
                var memory = httpContext.Response.BodyWriter.GetMemory(); // Shouldn't throw
                Assert.Equal(4096, memory.Length);

                await Task.CompletedTask;
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
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseCompleteGetMemoryReturnsRentedMemoryWithoutStartAsync()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.BodyWriter.Complete();
                var memory = httpContext.Response.BodyWriter.GetMemory(); // Shouldn't throw
                Assert.Equal(4096, memory.Length);

                await Task.CompletedTask;
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
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseGetMemoryAndStartAsyncMemoryReturnsNewMemory()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                var memory = httpContext.Response.BodyWriter.GetMemory();
                Assert.Equal(4096, memory.Length);

                await httpContext.Response.StartAsync();
                // Original memory is disposed, don't compare against it.

                memory = httpContext.Response.BodyWriter.GetMemory();
                Assert.NotEqual(4096, memory.Length);

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
                        "0",
                        "",
                        "");
                }
            }
        }


        [Fact]
        public async Task ResponseGetMemoryAndStartAsyncAdvanceThrows()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                var memory = httpContext.Response.BodyWriter.GetMemory();

                await httpContext.Response.StartAsync();

                Assert.Throws<InvalidOperationException>(() => httpContext.Response.BodyWriter.Advance(1));

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
                        "0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseCompleteGetMemoryDoesThrow()
        {
            InvalidOperationException writeEx = null;

            await using (var server = new TestServer(httpContext =>
            {
                httpContext.Response.BodyWriter.Complete();

                writeEx = Assert.Throws<InvalidOperationException>(() => httpContext.Response.BodyWriter.GetMemory());

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
                        "HTTP/1.1 200 OK",
                        $"Date: {server.Context.DateHeaderValue}",
                        "Content-Length: 0",
                        "",
                        "");
                }
            }

            Assert.NotNull(writeEx);
        }

        [Fact]
        public async Task ResponseSetBodyToSameValueTwiceGetPipeMultipleTimesDifferentObject()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Response.Body = new MemoryStream();
                var BodyWriter1 = httpContext.Response.BodyWriter;

                httpContext.Response.Body = new MemoryStream();
                var BodyWriter2 = httpContext.Response.BodyWriter;

                Assert.NotEqual(BodyWriter1, BodyWriter2);
                await Task.CompletedTask;
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
                        "Content-Length: 0",
                        "",
                        "");
                }
            }
        }

        [Fact]
        public async Task ResponseStreamWrappingWorks()
        {
            await using (var server = new TestServer(async httpContext =>
            {
                var oldBody = httpContext.Response.Body;
                httpContext.Response.Body = new MemoryStream();

                await httpContext.Response.BodyWriter.WriteAsync(new byte[1]);
                await httpContext.Response.Body.WriteAsync(new byte[1]);

                Assert.Equal(2, httpContext.Response.Body.Length);

                httpContext.Response.Body = oldBody;

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
                                        "Content-Length: 0",
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

            await using (var server = new TestServer(handler, new TestServiceContext(loggerFactory),
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
                Assert.Contains(testSink.Writes, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
                    && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
            }
            else
            {
                Assert.DoesNotContain(testSink.Writes, w => w.EventId.Id == 17);
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
