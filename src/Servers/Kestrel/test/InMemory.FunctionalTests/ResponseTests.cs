// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
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
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;
using BadHttpRequestException = Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class ResponseTests : TestApplicationErrorLoggerLoggedTest
{
    [Fact]
    public async Task OnCompleteCalledEvenWhenOnStartingNotCalled()
    {
        var onStartingCalled = false;
        TaskCompletionSource onCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(context =>
        {
            context.Response.OnStarting(() => Task.Run(() => onStartingCalled = true));
            context.Response.OnCompleted(() => Task.Run(() =>
            {
                onCompletedTcs.SetResult();
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));
        var cts = new CancellationTokenSource();
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeBlockedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

                writeBlockedTcs.TrySetResult();

                await writeTask;
            }
            catch (Exception ex)
            {
                appTcs.TrySetException(ex);
                writeBlockedTcs.TrySetException(ex);
            }
            finally
            {
                appTcs.TrySetResult();
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

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.WriteCanceled, m.Tags));
    }

    [Fact]
    public async Task BodyWriterWriteAsync_OnAbortedRequest_ReturnsResultWithIsCompletedTrue()
    {
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new TestServer(async context =>
        {
            try
            {
                context.Abort();
                var payload = Encoding.ASCII.GetBytes("hello world");
                var result = await context.Response.BodyWriter.WriteAsync(payload);
                Assert.True(result.IsCompleted);

                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        });
        using var connection = server.CreateConnection();
        await connection.Send(
            "GET / HTTP/1.1",
            "Host:",
            "",
            "");

        await appTcs.Task;
    }

    [Fact]
    public async Task BodyWriterWriteAsync_OnCanceledPendingFlush_ReturnsResultWithIsCanceled()
    {
        await using var server = new TestServer(async context =>
        {
            context.Response.BodyWriter.CancelPendingFlush();
            var payload = Encoding.ASCII.GetBytes("hello,");
            var cancelledResult = await context.Response.BodyWriter.WriteAsync(payload);
            Assert.True(cancelledResult.IsCanceled);

            var secondPayload = Encoding.ASCII.GetBytes(" world");
            var goodResult = await context.Response.BodyWriter.WriteAsync(secondPayload);
            Assert.False(goodResult.IsCanceled);
        });
        using var connection = server.CreateConnection();
        await connection.Send(
            "GET / HTTP/1.1",
            "Host:",
            "",
            "");

        await connection.Receive($"HTTP/1.1 200 OK",
            $"Date: {server.Context.DateHeaderValue}",
            "Transfer-Encoding: chunked",
            "",
            "6",
            "hello,"
            );
        await connection.Receive("",
            "6",
            " world"
            );
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
            expectedServerStatusCode: (HttpStatusCode)499);
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
            expectedServerStatusCode: (HttpStatusCode)499);
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
                catch (Microsoft.AspNetCore.Http.BadHttpRequestException)
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
        var delayTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

            delayTcs.SetResult();
        }
    }

    [Fact]
    public async Task InvalidChunkedEncodingInRequestShouldNotBlockOnCompleted()
    {
        var onCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(httpContext =>
        {
            httpContext.Response.OnCompleted(() => Task.Run(() =>
            {
                onCompletedTcs.SetResult();
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException readException = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async httpContext =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            readException = await Assert.ThrowsAsync<BadHttpRequestException>(
#pragma warning restore CS0618 // Type or member is obsolete
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(readException);

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Contains(TestSink.Writes, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
            && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
#pragma warning restore CS0618 // Type or member is obsolete
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    public static IEnumerable<object[]> Get1xxAnd204MethodCombinations()
    {
        // Status codes to test
        var statusCodes = new int[] {
                StatusCodes.Status100Continue,
                StatusCodes.Status101SwitchingProtocols,
                StatusCodes.Status102Processing,
                StatusCodes.Status204NoContent,
            };

        // HTTP methods to test
        var methods = new HttpMethod[] {
                HttpMethod.Connect,
                HttpMethod.Delete,
                HttpMethod.Get,
                HttpMethod.Head,
                HttpMethod.Options,
                HttpMethod.Patch,
                HttpMethod.Post,
                HttpMethod.Put,
                HttpMethod.Trace
            };

        foreach (var statusCode in statusCodes)
        {
            foreach (var method in methods)
            {
                yield return new object[] { statusCode, method };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Get1xxAnd204MethodCombinations))]
    public async Task AttemptingToWriteNonzeroContentLengthFailsFor1xxAnd204Responses(int statusCode, HttpMethod method)
        => await AttemptingToWriteNonzeroContentLengthFails(statusCode, method).ConfigureAwait(true);

    [Theory]
    [MemberData(nameof(Get1xxAnd204MethodCombinations))]
    public async Task AttemptingToWriteZeroContentLengthFor1xxAnd204Responses_ContentLengthRemoved(int statusCode, HttpMethod method)
        => await AttemptingToWriteZeroContentLength_ContentLengthRemoved(statusCode, method).ConfigureAwait(true);

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status201Created)]
    [InlineData(StatusCodes.Status202Accepted)]
    [InlineData(StatusCodes.Status203NonAuthoritative)]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(StatusCodes.Status205ResetContent)]
    [InlineData(StatusCodes.Status206PartialContent)]
    [InlineData(StatusCodes.Status207MultiStatus)]
    [InlineData(StatusCodes.Status208AlreadyReported)]
    [InlineData(StatusCodes.Status226IMUsed)]
    public async Task AttemptingToWriteNonzeroContentLengthFailsFor2xxResponsesOnConnect(int statusCode)
        => await AttemptingToWriteNonzeroContentLengthFails(statusCode, HttpMethod.Connect).ConfigureAwait(true);

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status201Created)]
    [InlineData(StatusCodes.Status202Accepted)]
    [InlineData(StatusCodes.Status203NonAuthoritative)]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(StatusCodes.Status205ResetContent)]
    [InlineData(StatusCodes.Status206PartialContent)]
    [InlineData(StatusCodes.Status207MultiStatus)]
    [InlineData(StatusCodes.Status208AlreadyReported)]
    [InlineData(StatusCodes.Status226IMUsed)]
    public async Task AttemptingToWriteZeroContentLengthFor2xxResponsesOnConnect_ContentLengthRemoved(int statusCode)
        => await AttemptingToWriteZeroContentLength_ContentLengthRemoved(statusCode, HttpMethod.Connect).ConfigureAwait(true);

    private async Task AttemptingToWriteNonzeroContentLengthFails(int statusCode, HttpMethod method)
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var responseWriteTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.Headers.ContentLength = 1;

            try
            {
                await httpContext.Response.StartAsync();
            }
            catch (Exception ex)
            {
                responseWriteTcs.TrySetException(ex);
                throw;
            }

            responseWriteTcs.TrySetResult();
        }, new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    $"{HttpUtilities.MethodToString(method)} / HTTP/1.1",
                    "Host:",
                    "",
                    "");

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => responseWriteTcs.Task).DefaultTimeout();
                Assert.Equal(CoreStrings.FormatHeaderNotAllowedOnResponse("Content-Length", statusCode), ex.Message);
            }
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    private async Task AttemptingToWriteZeroContentLength_ContentLengthRemoved(int statusCode, HttpMethod method)
    {
        var responseWriteTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.Headers.ContentLength = 0;

            try
            {
                await httpContext.Response.StartAsync();
            }
            catch (Exception ex)
            {
                responseWriteTcs.TrySetException(ex);
                throw;
            }

            responseWriteTcs.TrySetResult();
        }, new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    $"{HttpUtilities.MethodToString(method)} / HTTP/1.1",
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
    public async Task AttemptingToWriteNonzeroContentLengthFailsFor205Response()
    {
        var responseWriteTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            httpContext.Response.StatusCode = 205;
            httpContext.Response.Headers.ContentLength = 1;

            try
            {
                await httpContext.Response.StartAsync();
            }
            catch (Exception ex)
            {
                responseWriteTcs.TrySetException(ex);
                throw;
            }

            responseWriteTcs.TrySetResult();
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
                Assert.Equal(CoreStrings.NonzeroContentLengthNotAllowedOn205, ex.Message);
            }
        }
    }

    [Theory]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(StatusCodes.Status304NotModified)]
    public async Task AttemptingToWriteFailsForNonBodyResponse(int statusCode)
    {
        var responseWriteTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

            responseWriteTcs.TrySetResult();
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
        var responseWriteTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

            responseWriteTcs.TrySetResult();
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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

        var logTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ConnectionHeadResponseBodyWrite")
            {
                logTcs.SetResult();
            }
        };

        await using (var server = new TestServer(async httpContext =>
        {
            await httpContext.Response.WriteAsync(response);
            await httpContext.Response.BodyWriter.FlushAsync();
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

                // Wait for message to be logged before disposing the socket.
                // Disposing the socket will abort the connection and HttpProtocol._requestAborted
                // might be 1 by the time ProduceEnd() gets called and the message is logged.
                await logTcs.Task.DefaultTimeout();
            }
        }

        var logMessage = Assert.Single(LogMessages, message => message.EventId.Name == "ConnectionHeadResponseBodyWrite");

        Assert.Contains(
            @"write of ""12"" body bytes to non-body HEAD response.",
            logMessage.Message);
    }

    [Fact]
    public async Task ThrowsAndClosesConnectionWhenAppWritesMoreThanContentLengthWrite()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory))
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
                    "Content-Length: 11",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "hello,");

                await connection.WaitForConnectionClose();
            }
        }

        var logMessage = Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error);

        Assert.Equal(
            $"Response Content-Length mismatch: too many bytes written (12 of 11).",
            logMessage.Exception.Message);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.ResponseContentLengthMismatch, m.Tags));
    }

    [Fact]
    public async Task ThrowsAndClosesConnectionWhenAppWritesMoreThanContentLengthWriteAsync()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        var serviceContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

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
                    "Content-Length: 11",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "hello,");
            }
        }

        var logMessage = Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error);
        Assert.Equal(
            $"Response Content-Length mismatch: too many bytes written (12 of 11).",
            logMessage.Exception.Message);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.ResponseContentLengthMismatch, m.Tags));
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        var logMessage = Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error);
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        var logMessage = Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error);
        Assert.Equal(
            $"Response Content-Length mismatch: too many bytes written (12 of 5).",
            logMessage.Exception.Message);
    }

    [Fact]
    public async Task WhenAppWritesLessThanContentLengthErrorLogged()
    {
        var logTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ApplicationError")
            {
                logTcs.SetResult();
            }
        };

        await using (var server = new TestServer(async httpContext =>
        {
            httpContext.Response.ContentLength = 13;
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

                // Don't use ReceiveEnd here, otherwise the FIN might
                // abort the request before the server checks the
                // response content length, in which case the check
                // will be skipped.
                await connection.Receive(
                    $"HTTP/1.1 200 OK",
                    "Content-Length: 13",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "hello, world");

                // Wait for error message to be logged.
                await logTcs.Task.DefaultTimeout();

                // The server should close the connection in this situation.
                await connection.WaitForConnectionClose();
            }
        }

        Assert.Contains(TestSink.Writes,
           m => m.EventId.Name == "ApplicationError" &&
               m.Exception is InvalidOperationException ex &&
               ex.Message.Equals(CoreStrings.FormatTooFewBytesWritten(12, 13), StringComparison.Ordinal));
    }

    [Fact]
    public async Task WhenAppWritesLessThanContentLengthCompleteThrowsAndErrorLogged()
    {
        InvalidOperationException completeEx = null;

        var logTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {
            if (context.EventId.Name == "ApplicationError")
            {
                logTcs.SetResult();
            }
        };

        await using (var server = new TestServer(async httpContext =>
        {
            httpContext.Response.ContentLength = 13;
            await httpContext.Response.WriteAsync("hello, world");

            completeEx = Assert.Throws<InvalidOperationException>(() => httpContext.Response.BodyWriter.Complete());

        }, new TestServiceContext(LoggerFactory)))
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
                    "Content-Length: 13",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "hello, world");

                // Wait for error message to be logged.
                await logTcs.Task.DefaultTimeout();

                // The server should close the connection in this situation.
                await connection.WaitForConnectionClose();
            }
        }

        Assert.Contains(TestSink.Writes,
            m => m.EventId.Name == "ApplicationError" &&
                m.Exception is InvalidOperationException ex &&
                ex.Message.Equals(CoreStrings.FormatTooFewBytesWritten(12, 13), StringComparison.Ordinal));

        Assert.NotNull(completeEx);
    }

    [Fact]
    public async Task WhenAppWritesLessThanContentLengthButRequestIsAbortedErrorNotLogged()
    {
        var requestAborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            httpContext.RequestAborted.Register(() =>
            {
                requestAborted.SetResult();
            });

            httpContext.Response.ContentLength = 12;
            await httpContext.Response.WriteAsync("hello,");

            // Wait until the request is aborted so we know HttpProtocol will skip the response content length check.
            await requestAborted.Task.DefaultTimeout();
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
                    $"HTTP/1.1 200 OK",
                    "Content-Length: 12",
                    $"Date: {server.Context.DateHeaderValue}",
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
        Assert.Empty(TestSink.Writes.Where(c => c.EventId.Name == "ApplicationError"));
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "HTTP/1.1 500 Internal Server Error",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        var error = LogMessages.Where(message => message.LogLevel == LogLevel.Error);
        Assert.Equal(2, error.Count());
        Assert.All(error, message => message.Message.Equals(CoreStrings.FormatTooFewBytesWritten(0, 5)));
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.Empty(LogMessages.Where(message => message.LogLevel == LogLevel.Error));
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
                    "Content-Length: 13",
                    $"Date: {server.Context.DateHeaderValue}",
                    "Transfer-Encoding: chunked",
                    "",
                    "hello, world");
            }
        }

        Assert.Empty(LogMessages.Where(message => message.LogLevel == LogLevel.Error));
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
                    "Content-Length: 11",
                    $"Date: {server.Context.DateHeaderValue}",
                    "Transfer-Encoding: chunked",
                    "",
                    "hello, world");
            }
        }

        Assert.Empty(LogMessages.Where(message => message.LogLevel == LogLevel.Error));
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
                    "Content-Length: 42",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task HeadResponseBodyNotWrittenWithAsyncWrite()
    {
        var flushed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    "Content-Length: 12",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                flushed.SetResult();
            }
        }
    }

    [Fact]
    public async Task HeadResponseBodyNotWrittenWithSyncWrite()
    {
        var flushed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    "Content-Length: 12",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                flushed.SetResult();
            }
        }
    }

    [Fact]
    public async Task ZeroLengthWritesFlushHeaders()
    {
        var flushed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    "Content-Length: 12",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                flushed.SetResult();

                await connection.Receive("hello, world");
            }
        }
    }

    [Fact]
    public async Task AppCanWriteOwnBadRequestResponse()
    {
        var expectedResponse = string.Empty;
        var responseWritten = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            try
            {
                await httpContext.Request.Body.ReadAsync(new byte[1], 0, 1);
            }
            catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex)
            {
                expectedResponse = ex.Message;
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                httpContext.Response.ContentLength = ex.Message.Length;
                await httpContext.Response.WriteAsync(ex.Message);
                responseWritten.SetResult();
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
                    $"Content-Length: {expectedResponse.Length}",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Contains(LogMessages, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
            && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
#pragma warning restore CS0618 // Type or member is obsolete
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    while (LogMessages.TryDequeue(out var message))
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        if (message.EventId.Id == 17 && message.LogLevel <= LogLevel.Debug && message.Exception is BadHttpRequestException
                            && ((BadHttpRequestException)message.Exception).StatusCode == StatusCodes.Status400BadRequest)
#pragma warning restore CS0618 // Type or member is obsolete
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Contains(LogMessages, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
            && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
#pragma warning restore CS0618 // Type or member is obsolete
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    "Connection: keep-alive",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
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
                response.StatusCode = int.Parse(statusString, CultureInfo.InvariantCulture);
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
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
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 500 Internal Server Error",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.False(onStartingCalled);
        Assert.Equal(2, LogMessages.Where(message => message.LogLevel == LogLevel.Error).Count());

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 500 Internal Server Error",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }

        // The first registered OnStarting callback should have been called,
        // since they are called LIFO order and the other one failed.
        Assert.False(callback1Called);
        Assert.Equal(2, callback2CallCount);
        Assert.Equal(2, LogMessages.Where(message => message.LogLevel == LogLevel.Error).Count());
    }

    [Fact]
    public async Task OnStartingThrowsInsideOnStartingCallbacksRuns()
    {
        var testContext = new TestServiceContext(LoggerFactory);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            response.OnStarting(state1 =>
            {
                response.OnStarting(state2 =>
                {
                    tcs.TrySetResult();
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
                    "Content-Length: 11",
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
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            var response = httpContext.Response;
            response.OnCompleted(state1 =>
            {
                response.OnCompleted(state2 =>
                {
                    tcs.TrySetResult();

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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "");

                await tcs.Task.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task ThrowingInOnCompletedIsLogged()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }

        // All OnCompleted callbacks should be called even if they throw.
        Assert.Equal(2, LogMessages.Where(message => message.LogLevel == LogLevel.Error).Count());
        Assert.True(onCompletedCalled1);
        Assert.True(onCompletedCalled2);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.NoError(m.Tags));
    }

    [Fact]
    public async Task ThrowingAfterWritingKillsConnection()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }

        Assert.True(onStartingCalled);
        Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.ErrorAfterStartingResponse, m.Tags));
    }

    [Fact]
    public async Task ThrowingAfterPartialWriteKillsConnection()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello");
            }
        }

        Assert.True(onStartingCalled);
        Assert.Single(LogMessages, message => message.LogLevel == LogLevel.Error);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.ErrorAfterStartingResponse, m.Tags));
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
                    "Content-Length: 11",
                    "Connection: close",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World");
            }
        }

        Assert.Empty(LogMessages.Where(message => message.LogLevel == LogLevel.Error));
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
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");
        var testContext = new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory));

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

        Assert.Single(LogMessages.Where(m => m.Message.Contains(CoreStrings.ConnectionAbortedByApplication)));

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.AbortedByApp, m.Tags));
    }

    [Fact]
    public async Task AppAbortViaIConnectionLifetimeFeatureIsLogged()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        var closeTaskTcs = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async httpContext =>
        {
            var closeTask = await closeTaskTcs.Task.DefaultTimeout();
            var feature = httpContext.Features.Get<IConnectionLifetimeFeature>();
            feature.Abort();

            // Ensure the response doesn't get flush before the abort is observed.
            await closeTask;
        }, testContext))
        {
            using (var connection = server.CreateConnection())
            {
                closeTaskTcs.SetResult(connection.TransportConnection.WaitForCloseTask);

                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await connection.ReceiveEnd();
            }
        }

        Assert.Single(LogMessages.Where(m => m.Message.Contains("The connection was aborted by the application via IConnectionLifetimeFeature.Abort().")));
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task StartAsyncWithoutFlushingDoesNotFlush()
    {
        var testContext = new TestServiceContext(LoggerFactory);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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
                tcs.SetResult();
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
                    "Content-Length: 11",
                    $"Date: {testContext.DateHeaderValue}",
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
                    $"Content-Length: {expectedLength}",
                    $"Date: {testContext.DateHeaderValue}",
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
                    $"Content-Length: {expectedLength}",
                    $"Date: {testContext.DateHeaderValue}",
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
        var onStartingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async context =>
        {
            context.Response.OnStarting(_ =>
            {
                callOrder.Push(1);
                onStartingTcs.SetResult();
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
                    $"Content-Length: {response.Length}",
                    $"Date: {testContext.DateHeaderValue}",
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
        var onCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var server = new TestServer(async context =>
        {
            context.Response.OnCompleted(_ =>
            {
                callOrder.Push(1);
                onCompletedTcs.SetResult();
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
                    $"Content-Length: {response.Length}",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 6",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 6",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 6",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 6",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 42",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 12",
                    $"Date: {testContext.DateHeaderValue}",
                    "",
                    "Hello World!");
            }
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
                    "Content-Length: 12",
                    $"Date: {testContext.DateHeaderValue}",
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
                    "Content-Length: 12",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 54",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task ResponseBodyWriterCompleteWithoutExceptionNextWriteDoesThrow()
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(writeEx);
    }

    [Fact]
    public async Task ResponseBodyWriterCompleteFlushesChunkTerminator()
    {
        var middlewareCompletionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var server = new TestServer(async httpContext =>
        {
            await httpContext.Response.WriteAsync("hello, world");
            await httpContext.Response.BodyWriter.CompleteAsync();
            await middlewareCompletionTcs.Task;
        }, new TestServiceContext(LoggerFactory));

        using var connection = server.CreateConnection();

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
            "c",
            "hello, world",
            "0",
            "",
            "");

        middlewareCompletionTcs.SetResult();
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                        "Content-Length: 1",
                        $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");

                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
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
                                    "Content-Length: 0",
                                    $"Date: {server.Context.DateHeaderValue}",
                                    "",
                                    "");
            }
        }
    }

    [Fact]
    public async Task AltSvc_HeaderSetInAppCode_AltSvcNotOverwritten()
    {
        await using (var server = new TestServer(
            httpContext =>
            {
                httpContext.Response.Headers.AltSvc = "Custom";
                return Task.CompletedTask;
            },
            new TestServiceContext(LoggerFactory),
            options =>
            {
                options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                {
                    Protocols = HttpProtocols.Http1AndHttp2AndHttp3
                });
            },
            services => { }))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    @"Alt-Svc: Custom",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task AltSvc_Http1And2And3EndpointConfigured_AltSvcInResponseHeaders()
    {
        await using (var server = new TestServer(
            httpContext => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            options =>
            {
                options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                {
                    Protocols = HttpProtocols.Http1AndHttp2AndHttp3,
                    IsTls = true
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(new MockMultiplexedConnectionListenerFactory());
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    @"Alt-Svc: h3="":0""; ma=86400",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task AltSvc_Http1And2And3EndpointConfigured_NoMultiplexedFactory_NoAltSvcInResponseHeaders()
    {
        await using (var server = new TestServer(
            httpContext => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            options =>
            {
                options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                {
                    Protocols = HttpProtocols.Http1AndHttp2AndHttp3,
                    IsTls = true
                });
            },
            services => { }))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task AltSvc_Http1_NoAltSvcInResponseHeaders()
    {
        await using (var server = new TestServer(
            httpContext => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0)) { Protocols = HttpProtocols.Http1 }))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task AltSvc_Http3ConfiguredDifferentEndpoint_NoAltSvcInResponseHeaders()
    {
        await using (var server = new TestServer(
            httpContext => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            options =>
            {
                options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                {
                    Protocols = HttpProtocols.Http1
                });
                options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 1))
                {
                    Protocols = HttpProtocols.Http3,
                    IsTls = true
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(new MockMultiplexedConnectionListenerFactory());
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task AltSvc_DisableAltSvcHeaderIsTrue_Http1And2And3EndpointConfigured_NoAltSvcInResponseHeaders()
    {
        await using (var server = new TestServer(
            httpContext => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            options =>
            {
                options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))
                {
                    Protocols = HttpProtocols.Http1AndHttp2AndHttp3,
                    DisableAltSvcHeader = true
                });
            },
            services => { }))
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
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task WriteBeforeFlushingHeadersTracksBytesCorrectly()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new TestServer(async context =>
        {
            try
            {
                var length = 0;
                var memory = context.Response.BodyWriter.GetMemory();
                context.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;
                Assert.Equal(length, context.Response.BodyWriter.UnflushedBytes);

                memory = context.Response.BodyWriter.GetMemory();
                context.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;

                Assert.Equal(length, context.Response.BodyWriter.UnflushedBytes);

                await context.Response.BodyWriter.FlushAsync();

                Assert.Equal(0, context.Response.BodyWriter.UnflushedBytes);

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

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
                "");

            await tcs.Task;
        }
    }

    [Fact]
    public async Task WriteAfterFlushingHeadersTracksBytesCorrectly()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new TestServer(async context =>
        {
            try
            {
                await context.Response.StartAsync();
                // StartAsync doesn't actually flush, it just commits bytes to Pipe
                // going to flush here so we have 0 unflushed bytes to make asserts below easier
                await context.Response.BodyWriter.FlushAsync();

                var length = 0;
                var memory = context.Response.BodyWriter.GetMemory();
                context.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;
                Assert.Equal(length, context.Response.BodyWriter.UnflushedBytes);

                memory = context.Response.BodyWriter.GetMemory();
                context.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;

                // + 7 for first chunked framing (ff9\r\n<data>\r\n)
                Assert.Equal(length + 7, context.Response.BodyWriter.UnflushedBytes);

                await context.Response.BodyWriter.FlushAsync();

                Assert.Equal(0, context.Response.BodyWriter.UnflushedBytes);

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

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
                "");

            await tcs.Task;
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
            options => options.CodeBackedListenOptions.Add(new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0))),
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
                            "Content-Length: 0",
                            $"Date: {server.Context.DateHeaderValue}",
                            "",
                            "");
                    }
                    else
                    {
                        await connection.ReceiveEnd(
                            "HTTP/1.1 400 Bad Request",
                            "Content-Length: 0",
                            "Connection: close",
                            $"Date: {server.Context.DateHeaderValue}",
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
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Contains(testSink.Writes, w => w.EventId.Id == 17 && w.LogLevel <= LogLevel.Debug && w.Exception is BadHttpRequestException
                && ((BadHttpRequestException)w.Exception).StatusCode == StatusCodes.Status400BadRequest);
#pragma warning restore CS0618 // Type or member is obsolete
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

        return (HttpStatusCode)int.Parse(response.Substring(statusStart, statusLength), CultureInfo.InvariantCulture);
    }
}
