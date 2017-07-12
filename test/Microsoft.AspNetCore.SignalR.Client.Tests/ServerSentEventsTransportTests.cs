// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class ServerSentEventsTransportTests
    {
        [Fact]
        public async Task CanStartStopSSETransport()
        {
            var eventStreamTcs = new TaskCompletionSource<object>();
            var copyToAsyncTcs = new TaskCompletionSource<int>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    // Receive loop started - allow stopping the transport
                    eventStreamTcs.SetResult(null);

                    // returns unfinished task to block pipelines
                    var mockStream = new Mock<Stream>();
                    mockStream
                        .Setup(s => s.CopyToAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns(copyToAsyncTcs.Task);
                    return new HttpResponseMessage { Content = new StreamContent(mockStream.Object) };
                });

            try
            {
                using (var httpClient = new HttpClient(mockHttpHandler.Object))
                {
                    var sseTransport = new ServerSentEventsTransport(httpClient);
                    var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                    var transportToConnection = Channel.CreateUnbounded<byte[]>();
                    var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);
                    await sseTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, connectionId: string.Empty).OrTimeout();

                    await eventStreamTcs.Task.OrTimeout();
                    await sseTransport.StopAsync().OrTimeout();
                    await sseTransport.Running.OrTimeout();
                }
            }
            finally
            {
                copyToAsyncTcs.SetResult(0);
            }
        }
    }
}
