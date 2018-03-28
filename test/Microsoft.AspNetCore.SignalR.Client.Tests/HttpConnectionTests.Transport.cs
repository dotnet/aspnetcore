// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        public class Transport
        {
            [Fact]
            public async Task CanReceiveData()
            {
                var testHttpHandler = new TestHttpMessageHandler();

                // Set the long poll up to return a single message over a few polls.
                var requestCount = 0;
                var messageFragments = new[] {"This ", "is ", "a ", "test"};
                testHttpHandler.OnLongPoll(cancellationToken =>
                {
                    if (requestCount >= messageFragments.Length)
                    {
                        return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                    }

                    var resp = ResponseUtils.CreateResponse(HttpStatusCode.OK, messageFragments[requestCount]);
                    requestCount += 1;
                    return resp;
                });
                testHttpHandler.OnSocketSend((_, __) => ResponseUtils.CreateResponse(HttpStatusCode.Accepted));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        Assert.Contains("This is a test", Encoding.UTF8.GetString(await connection.Transport.Input.ReadAllAsync()));
                    });
            }

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
                    async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();

                        await connection.Transport.Output.WriteAsync(data).OrTimeout();

                        Assert.Equal(data, await sendTcs.Task.OrTimeout());

                        longPollTcs.TrySetResult(ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                    });
            }

            [Fact]
            public Task SendThrowsIfConnectionIsNotStarted()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection) =>
                    {
                        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                            () => connection.Transport.Output.WriteAsync(new byte[0]).OrTimeout());
                        Assert.Equal($"Cannot access the {nameof(Transport)} pipe before the connection has started.", exception.Message);
                    });
            }

            [Fact]
            public Task TransportPipeCannotBeAccessedAfterConnectionIsDisposed()
            {
                return WithConnectionAsync(
                    CreateConnection(),
                    async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await connection.DisposeAsync().OrTimeout();

                        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
                            () => connection.Transport.Output.WriteAsync(new byte[0]).OrTimeout());
                        Assert.Equal(nameof(HttpConnection), exception.ObjectName);
                    });
            }

            [Fact]
            public Task TransportIsShutDownAfterDispose()
            {
                var transport = new TestTransport();
                return WithConnectionAsync(
                    CreateConnection(transport: transport),
                    async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        await connection.DisposeAsync().OrTimeout();

                        // This will throw OperationCancelledException if it's forcibly terminated
                        // which we don't want
                        await transport.Receiving.OrTimeout();
                    });
            }
        }
    }
}
