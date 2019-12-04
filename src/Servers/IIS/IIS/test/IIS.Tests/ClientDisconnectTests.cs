// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [SkipIfHostableWebCoreNotAvailable]
    [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, "https://github.com/aspnet/IISIntegration/issues/866")]
    public class ClientDisconnectTests : StrictTestServerTests
    {
        [ConditionalFact]
        public async Task WritesSucceedAfterClientDisconnect()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var clientDisconnectedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            var data = new byte[1024];
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    requestStartedCompletionSource.SetResult(true);
                    await clientDisconnectedCompletionSource.Task;
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
                clientDisconnectedCompletionSource.SetResult(true);

                await requestCompletedCompletionSource.Task.DefaultTimeout();
            }

            AssertConnectionDisconnectLog();
        }

        [ConditionalFact]
        public async Task WritesCancelledWhenUsingAbortedToken()
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
        public async Task WriterThrowsCancelledException()
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
        public async Task ReaderThrowsCancelledException()
        {
            var requestStartedCompletionSource = CreateTaskCompletionSource();
            var requestCompletedCompletionSource = CreateTaskCompletionSource();

            Exception exception = null;
            var cancellationTokenSource = new CancellationTokenSource();

            var data = new byte[1024];
            using (var testServer = await TestServer.Create(async ctx =>
            {
                requestStartedCompletionSource.SetResult(true);
                try
                {
                    await ctx.Request.Body.ReadAsync(data, cancellationTokenSource.Token);
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
        public async Task RequestAbortedIsTrippedWithoutIO()
        {
            var requestStarted = CreateTaskCompletionSource();
            var requestAborted = CreateTaskCompletionSource();

            using (var testServer = await TestServer.Create(
                async ctx => {
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
