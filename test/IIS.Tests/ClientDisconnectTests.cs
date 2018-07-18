// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [SkipIfHostableWebCoreNotAvailable]
    [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, "https://github.com/aspnet/IISIntegration/issues/866")]
    public class ClientDisconnectTests : LoggedTest
    {
         
        [ConditionalFact]
        public async Task WritesSucceedAfterClientDisconnect()
        {
            var requestStartedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var clientDisconnectedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestCompletedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    await requestStartedCompletionSource.Task.TimeoutAfterDefault();
                }
                clientDisconnectedCompletionSource.SetResult(true);

                await requestCompletedCompletionSource.Task.TimeoutAfterDefault();
            }
        }

        [ConditionalFact]
        public async Task ReadThrowsAfterClientDisconnect()
        {
            var requestStartedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestCompletedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    await requestStartedCompletionSource.Task.TimeoutAfterDefault();
                }

                await requestCompletedCompletionSource.Task.TimeoutAfterDefault();
            }

            Assert.IsType<IOException>(exception);
            Assert.Equal("Native IO operation failed", exception.Message);
        }

        [ConditionalFact]
        public async Task WriterThrowsCancelledException()
        {
            var requestStartedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestCompletedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Exception exception = null;
            var cancellationTokenSource = new CancellationTokenSource();

            var data = new byte[1024];
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

                    await requestStartedCompletionSource.Task.TimeoutAfterDefault();
                    cancellationTokenSource.Cancel();
                    await requestCompletedCompletionSource.Task.TimeoutAfterDefault();
                }

                Assert.IsType<OperationCanceledException>(exception);
            }
        }

        [ConditionalFact]
        public async Task ReaderThrowsCancelledException()
        {
            var requestStartedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestCompletedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
                    await requestStartedCompletionSource.Task.TimeoutAfterDefault();
                    cancellationTokenSource.Cancel();
                    await requestCompletedCompletionSource.Task.TimeoutAfterDefault();
                }
                Assert.IsType<OperationCanceledException>(exception);
            }
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
