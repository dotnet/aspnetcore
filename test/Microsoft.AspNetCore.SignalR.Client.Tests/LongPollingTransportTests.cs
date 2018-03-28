// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class LongPollingTransportTests
    {
        [Fact]
        public async Task LongPollingTransportStopsPollAndSendLoopsWhenTransportStopped()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                    {
                        await Task.Yield();
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    });

            Task transportActiveTask;

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    transportActiveTask = longPollingTransport.Running;

                    Assert.False(transportActiveTask.IsCompleted);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }

                await transportActiveTask.OrTimeout();
            }
        }

        [Fact]
        public async Task LongPollingTransportStopsWhenPollReceives204()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {

                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    await longPollingTransport.Running.OrTimeout();

                    Assert.True(pair.Transport.Input.TryRead(out var result));
                    Assert.True(result.IsCompleted);
                    pair.Transport.Input.AdvanceTo(result.Buffer.End);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportResponseWithNoContentDoesNotStopPoll()
        {
            int requests = 0;
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    if (requests == 0)
                    {
                        requests++;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK, "Hello");
                    }
                    else if (requests == 1)
                    {
                        requests++;
                        // Time out
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    }
                    else if (requests == 2)
                    {
                        requests++;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK, "World");
                    }

                    // Done
                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {

                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    var data = await pair.Transport.Input.ReadAllAsync().OrTimeout();
                    await longPollingTransport.Running.OrTimeout();
                    Assert.Equal(Encoding.UTF8.GetBytes("HelloWorld"), data);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportStopsWhenPollRequestFails()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    var exception =
                        await Assert.ThrowsAsync<HttpRequestException>(async () =>
                        {
                            async Task ReadAsync()
                            {
                                await pair.Transport.Input.ReadAsync();
                            }

                            await ReadAsync().OrTimeout();
                        });
                    Assert.Contains(" 500 ", exception.Message);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportStopsWhenSendRequestFails()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    var statusCode = request.Method == HttpMethod.Post
                        ? HttpStatusCode.InternalServerError
                        : HttpStatusCode.OK;
                    return ResponseUtils.CreateResponse(statusCode);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    await pair.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

                    await Assert.ThrowsAsync<HttpRequestException>(async () => await longPollingTransport.Running.OrTimeout());

                    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await pair.Transport.Input.ReadAllAsync().OrTimeout());
                    Assert.Contains(" 500 ", exception.Message);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportShutsDownWhenChannelIsClosed()
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    pair.Transport.Output.Complete();

                    await longPollingTransport.Running.OrTimeout();

                    await pair.Transport.Input.ReadAllAsync().OrTimeout();
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportDispatchesMessagesReceivedFromPoll()
        {
            var message1Payload = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };

            var firstCall = true;
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            var sentRequests = new List<HttpRequestMessage>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    sentRequests.Add(request);

                    await Task.Yield();

                    if (firstCall)
                    {
                        firstCall = false;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK, message1Payload);
                    }

                    return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                    // Start the transport
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    // Wait for the transport to finish
                    await longPollingTransport.Running.OrTimeout();

                    // Pull Messages out of the channel
                    var message = await pair.Transport.Input.ReadAllAsync();

                    // Check the provided request
                    Assert.Equal(2, sentRequests.Count);

                    // Check the messages received
                    Assert.Equal(message1Payload, message);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportSendsAvailableMessagesWhenTheyArrive()
        {
            var sentRequests = new List<byte[]>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    if (request.Method == HttpMethod.Post)
                    {
                        // Build a new request object, but convert the entire payload to string
                        sentRequests.Add(await request.Content.ReadAsByteArrayAsync());
                    }
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                    // Pre-queue some messages
                    await pair.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
                    await pair.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("World"));

                    // Start the transport
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    pair.Transport.Output.Complete();

                    await longPollingTransport.Running.OrTimeout();
                    await pair.Transport.Input.ReadAllAsync();

                    Assert.Single(sentRequests);
                    Assert.Equal(new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d'
                    }, sentRequests[0]);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Theory]
        [InlineData(TransferFormat.Binary)]
        [InlineData(TransferFormat.Text)]
        public async Task LongPollingTransportSetsTransferFormat(TransferFormat transferFormat)
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, transferFormat, connection: new TestConnection());
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }

        [Fact]
        public async Task LongPollingTransportSetsUserAgent()
        {
            HttpHeaderValueCollection<ProductInfoHeaderValue> userAgentHeaderCollection = null;

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    userAgentHeaderCollection = request.Headers.UserAgent;
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Text, connection: new TestConnection());
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }

            Assert.NotNull(userAgentHeaderCollection);
            var userAgentHeader = Assert.Single(userAgentHeaderCollection);
            Assert.Equal("Microsoft.AspNetCore.Sockets.Client.Http", userAgentHeader.Product.Name);

            // user agent version should come from version embedded in assembly metadata
            var assemblyVersion = typeof(Constants)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            Assert.Equal(assemblyVersion.InformationalVersion, userAgentHeader.Product.Version);
        }

        [Theory]
        [InlineData(TransferFormat.Text | TransferFormat.Binary)] // Multiple values not allowed
        [InlineData((TransferFormat)42)] // Unexpected value
        public async Task LongPollingTransportThrowsForInvalidTransferFormat(TransferFormat transferFormat)
        {
            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), null, transferFormat, connection: new TestConnection()));

                Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
                Assert.Equal("transferFormat", exception.ParamName);
            }
        }

        [Fact]
        public async Task LongPollingTransportRePollsIfRequestCancelled()
        {
            var numPolls = 0;
            var completionTcs = new TaskCompletionSource<object>();

            var mockHttpHandler = new Mock<HttpMessageHandler>();
            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    await Task.Yield();

                    if (Interlocked.Increment(ref numPolls) < 3)
                    {
                        throw new OperationCanceledException();
                    }

                    completionTcs.SetResult(null);
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                });

            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient);

                try
                {
                    var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                    await longPollingTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferFormat.Binary, connection: new TestConnection());

                    var completedTask = await Task.WhenAny(completionTcs.Task, longPollingTransport.Running).OrTimeout();
                    Assert.Equal(completionTcs.Task, completedTask);
                }
                finally
                {
                    await longPollingTransport.StopAsync();
                }
            }
        }
    }
}
