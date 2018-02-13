// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        public class SendAsync
        {
            [Fact]
            public async Task CanSendData()
            {
                var data = new byte[] { 1, 1, 2, 3, 5, 8 };

                var testHttpHandler = new TestHttpMessageHandler();

                var sendTcs = new TaskCompletionSource<byte[]>();
                var longPollTcs = new TaskCompletionSource<HttpResponseMessage>();

                testHttpHandler.OnLongPoll(cancellationToken => longPollTcs.Task);

                testHttpHandler.OnSocketSend((buf, cancellationToken) =>
                {
                    sendTcs.TrySetResult(buf);
                    return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.Accepted));
                });

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection, closed) =>
                    {
                        await connection.StartAsync().OrTimeout();

                        await connection.SendAsync(data).OrTimeout();

                        Assert.Equal(data, await sendTcs.Task.OrTimeout());

                        longPollTcs.TrySetResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                    });
            }

            [Fact]
            public Task SendThrowsIfConnectionIsNotStarted()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection, closed) =>
                    {
                        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                            () => connection.SendAsync(new byte[0]).OrTimeout());
                        Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
                    });
            }

            [Fact]
            public Task SendThrowsIfConnectionIsStopped()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection, closed) =>
                    {
                        await connection.StartAsync().OrTimeout();
                        await connection.StopAsync().OrTimeout();

                        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                            () => connection.SendAsync(new byte[0]).OrTimeout());
                        Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
                    });
            }

            [Fact]
            public Task SendThrowsIfConnectionIsDisposed()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection, closed) =>
                    {
                        await connection.StartAsync().OrTimeout();
                        await connection.DisposeAsync().OrTimeout();

                        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                            () => connection.SendAsync(new byte[0]).OrTimeout());
                        Assert.Equal("Cannot send messages when the connection is not in the Connected state.", exception.Message);
                    });
            }

            [Fact]
            public async Task ExceptionOnSendAsyncClosesWithError()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                var longPollTcs = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

                testHttpHandler.OnLongPoll(cancellationToken =>
                {
                    cancellationToken.Register(() => longPollTcs.TrySetResult(null));

                    return longPollTcs.Task;
                });

                testHttpHandler.OnSocketSend((buf, cancellationToken) =>
                {
                    return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError));
                });

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection, closed) =>
                    {
                        await connection.StartAsync().OrTimeout();

                        await connection.SendAsync(new byte[] { 0 }).OrTimeout();

                        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => closed.OrTimeout());
                    });
            }
        }
    }
}
