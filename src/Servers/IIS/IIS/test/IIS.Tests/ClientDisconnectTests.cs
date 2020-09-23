// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [SkipIfHostableWebCoreNotAvailable]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
    public class ClientDisconnectTests : StrictTestServerTests
    {
        [ConditionalFact]
        public async Task WritesSucceedAfterClientDisconnect()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();
            var requestAborted = CreateTaskCompletionSource();

            var data = new byte[1024];
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    requestStartedCompletionSource.SetResult(true);
                    ctx.RequestAborted.Register(() => requestAborted.SetResult(true));

                    await requestAborted.Task.DefaultTimeout();
                    for (var i = 0; i < 1000; i++)
                    {
                        await ctx.Response.Body.WriteAsync(data);
                    }

                    requestCompletedCompletionSource.SetResult(true);
                }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await SendContentLength1Post(connection);
                    await requestStartedCompletionSource.Task.DefaultTimeout();
                }

                await requestAborted.Task.DefaultTimeout();

                await requestCompletedCompletionSource.Task.DefaultTimeout();
            }

            AssertConnectionDisconnectLog();
        }

        [ConditionalFact]
        public async Task WritesCanceledWhenUsingAbortedToken()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            Exception exception = null;

            var data = new byte[1];
            using (var testServer = await TestServer.Create(async ctx =>
            {
                requestStartedCompletionSource.SetResult(true);
                try
                {
                    while (true)
                    {
                        await ctx.Response.Body.WriteAsync(data, ctx.RequestAborted);
                        await Task.Delay(10); // Small delay to not constantly call WriteAsync.
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                requestCompletedCompletionSource.SetResult(true);
            }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await SendContentLength1Post(connection);

                    await requestStartedCompletionSource.Task.DefaultTimeout();
                }

                await requestCompletedCompletionSource.Task.DefaultTimeout();

                Assert.IsType<OperationCanceledException>(exception);
            }

            AssertConnectionDisconnectLog();
        }

        [ConditionalFact]
        public async Task ReadThrowsAfterClientDisconnect()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            Exception exception = null;

            var data = new byte[1024];
            using (var testServer = await TestServer.Create(async ctx =>
            {
                requestStartedCompletionSource.SetResult(true);
                try
                {
                    await ctx.Request.Body.ReadAsync(data);
                }
                catch (Exception e)
                {
                    exception = e;
                }

                requestCompletedCompletionSource.SetResult(true);
            }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await SendContentLength1Post(connection);
                    await requestStartedCompletionSource.Task.DefaultTimeout();
                }

                await requestCompletedCompletionSource.Task.DefaultTimeout();
            }

            Assert.IsType<ConnectionResetException>(exception);
            Assert.Equal("The client has disconnected", exception.Message);

            AssertConnectionDisconnectLog();
        }

        [ConditionalFact]
        public async Task WriterThrowsCanceledException()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            Exception exception = null;
            var cancellationTokenSource = new CancellationTokenSource();

            var data = new byte[1];
            using (var testServer = await TestServer.Create(async ctx =>
            {
                requestStartedCompletionSource.SetResult(true);
                try
                {
                    while (true)
                    {
                        await ctx.Response.Body.WriteAsync(data, cancellationTokenSource.Token);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                requestCompletedCompletionSource.SetResult(true);
            }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await SendContentLength1Post(connection);

                    await requestStartedCompletionSource.Task.DefaultTimeout();
                    cancellationTokenSource.Cancel();
                    await requestCompletedCompletionSource.Task.DefaultTimeout();
                }

                Assert.IsType<OperationCanceledException>(exception);
            }
        }

        [ConditionalFact]
        [Repeat]
        public async Task ReaderThrowsCanceledException()
        {
            var readIsAsyncCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            Exception exception = null;
            var cancellationTokenSource = new CancellationTokenSource();

            var data = new byte[1024];
            using (var testServer = await TestServer.Create(async ctx =>
            {
                try
                {
                    var task = ctx.Request.Body.ReadAsync(data, cancellationTokenSource.Token);
                    readIsAsyncCompletionSource.SetResult(true);
                    await task;
                }
                catch (Exception e)
                {
                    exception = e;
                }

                requestCompletedCompletionSource.SetResult(true);
            }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await SendContentLength1Post(connection);
                    await readIsAsyncCompletionSource.Task.DefaultTimeout();
                    cancellationTokenSource.Cancel();
                    await requestCompletedCompletionSource.Task.DefaultTimeout();
                }

                try
                {
                    Assert.IsType<OperationCanceledException>(exception);
                }
                catch (Exception)
                {
                    Logger.LogError(exception, "Unexpected exception type");
                    throw;
                }
            }
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/1817")]
        public async Task ReaderThrowsResetExceptionOnInvalidBody()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            Exception exception = null;

            var data = new byte[1024];
            using (var testServer = await TestServer.Create(async ctx =>
            {
                requestStartedCompletionSource.SetResult(true);
                try
                {
                    await ctx.Request.Body.ReadAsync(data);
                }
                catch (Exception e)
                {
                    exception = e;
                }
                requestCompletedCompletionSource.SetResult(true);
            }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "Host: localhost",
                        "Connection: close",
                        "",
                        "");

                    await requestStartedCompletionSource.Task;
                    await connection.Send(
                        "ZZZZZZZZZZZZZ");

                    await connection.Receive(
                        "HTTP/1.1 400 Bad Request",
                        ""
                        );

                }
                await requestCompletedCompletionSource.Task.DefaultTimeout();
            }

            Assert.IsType<ConnectionResetException>(exception);
            Assert.Equal("The client has disconnected", exception.Message);
            AssertConnectionDisconnectLog();
        }

        [ConditionalFact]
        public async Task ReadsAlwaysGoAsync()
        {
            // A hypothesis on why there are flaky tests is due to read async not going
            // async. Adding a test that confirms ReadAsync is async.
            for (var i = 0; i < 10; i++)
            {
                var requestStartedCompletionSource = CreateTaskCompletionSource();
                var requestCompletedCompletionSource = CreateTaskCompletionSource();

                var data = new byte[1024];
                using (var testServer = await TestServer.Create(async ctx =>
                {
                    var task = ctx.Request.Body.ReadAsync(data);
                    Assert.True(!task.IsCompleted);
                    requestStartedCompletionSource.SetResult(true);
                    await task;

                    requestCompletedCompletionSource.SetResult(true);
                }, LoggerFactory))
                {
                    using (var connection = testServer.CreateConnection())
                    {
                        await SendContentLength1Post(connection);

                        await requestStartedCompletionSource.Task;
                        await connection.Send(
                            "a");

                        await connection.Receive(
                            "HTTP/1.1 200 OK",
                            ""
                            );

                    }
                    await requestCompletedCompletionSource.Task.DefaultTimeout();
                }
            }
        }

        [ConditionalFact]
        public async Task RequestAbortedIsTrippedWithoutIO()
        {
            var requestStarted = CreateTaskCompletionSource();
            var requestAborted = CreateTaskCompletionSource();

            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    ctx.RequestAborted.Register(() => requestAborted.SetResult(true));
                    requestStarted.SetResult(true);
                    await requestAborted.Task;
                }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await SendContentLength1Post(connection);
                    await requestStarted.Task;
                }
                await requestAborted.Task;
            }

            AssertConnectionDisconnectLog();
        }

        private void AssertConnectionDisconnectLog()
        {
            Assert.Single(TestSink.Writes, w => w.EventId.Name == "ConnectionDisconnect");
        }

        private static async Task SendContentLength1Post(TestConnection connection)
        {
            await connection.Send(
                "POST / HTTP/1.1",
                "Content-Length: 1",
                "Host: localhost",
                "Connection: close",
                "",
                "");
        }
    }
}
