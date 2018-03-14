// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Sockets;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        public class OnReceived
        {
            [Fact]
            public async Task CanReceiveData()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                testHttpHandler.OnLongPoll(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.OK, "42"));
                testHttpHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection, closed) =>
                    {
                        var receiveTcs = new TaskCompletionSource<string>();
                        connection.OnReceived((data, state) =>
                        {
                            var tcs = ((TaskCompletionSource<string>)state);
                            tcs.TrySetResult(Encoding.UTF8.GetString(data));
                            return Task.CompletedTask;
                        }, receiveTcs);

                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        Assert.Contains("42", await receiveTcs.Task.OrTimeout());
                    });
            }

            [Fact]
            public async Task CanReceiveDataEvenIfExceptionThrownFromPreviousReceivedEvent()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                testHttpHandler.OnLongPoll(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.OK, "42"));
                testHttpHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection, closed) =>
                    {
                        var receiveTcs = new TaskCompletionSource<string>();
                        var receivedRaised = false;
                        connection.OnReceived((data, state) =>
                        {
                            if (!receivedRaised)
                            {
                                receivedRaised = true;
                                return Task.FromException(new InvalidOperationException());
                            }

                            receiveTcs.TrySetResult(Encoding.UTF8.GetString(data));
                            return Task.CompletedTask;
                        }, receiveTcs);

                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        Assert.Contains("42", await receiveTcs.Task.OrTimeout());
                        Assert.True(receivedRaised);
                    });
            }

            [Fact]
            public async Task CanReceiveDataEvenIfExceptionThrownSynchronouslyFromPreviousReceivedEvent()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                testHttpHandler.OnLongPoll(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.OK, "42"));
                testHttpHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection, closed) =>
                    {
                        var receiveTcs = new TaskCompletionSource<string>();
                        var receivedRaised = false;
                        connection.OnReceived((data, state) =>
                        {
                            if (!receivedRaised)
                            {
                                receivedRaised = true;
                                throw new InvalidOperationException();
                            }

                            receiveTcs.TrySetResult(Encoding.UTF8.GetString(data));
                            return Task.CompletedTask;
                        }, receiveTcs);

                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        Assert.Contains("42", await receiveTcs.Task.OrTimeout());
                        Assert.True(receivedRaised);
                    });
            }
        }
    }
}
