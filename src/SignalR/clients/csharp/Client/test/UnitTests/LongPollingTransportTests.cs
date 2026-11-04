// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class LongPollingTransportTests : VerifiableLoggedTest
{
    private static readonly Uri TestUri = new Uri("http://example.com/?id=1234");

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
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                transportActiveTask = longPollingTransport.Running;

                Assert.False(transportActiveTask.IsCompleted);
            }
            finally
            {
                await longPollingTransport.StopAsync();
            }

            await transportActiveTask.DefaultTimeout();
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
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                await longPollingTransport.Running.DefaultTimeout();

                Assert.True(longPollingTransport.Input.TryRead(out var result));
                Assert.True(result.IsCompleted);
                longPollingTransport.Input.AdvanceTo(result.Buffer.End);
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
        var requests = 0;
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();

                if (requests == 0)
                {
                    requests++;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
                else if (requests == 1)
                {
                    requests++;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK, "Hello");
                }
                else if (requests == 2)
                {
                    requests++;
                    // Time out
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
                else if (requests == 3)
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
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                var data = await longPollingTransport.Input.ReadAllAsync().DefaultTimeout();
                await longPollingTransport.Running.DefaultTimeout();
                Assert.Equal(Encoding.UTF8.GetBytes("HelloWorld"), data);
            }
            finally
            {
                await longPollingTransport.StopAsync();
            }
        }
    }

    [Fact]
    public async Task LongPollingTransportStartAsyncFailsIfFirstRequestFails()
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
                var exception = await Assert.ThrowsAsync<HttpRequestException>(() => longPollingTransport.StartAsync(TestUri, TransferFormat.Binary));
                Assert.Contains(" 500 ", exception.Message);
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
        var firstPoll = true;
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();
                if (firstPoll)
                {
                    firstPoll = false;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
                return ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError);
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);
            try
            {
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                var exception =
                    await Assert.ThrowsAsync<HttpRequestException>(async () =>
                    {
                        async Task ReadAsync()
                        {
                            await longPollingTransport.Input.ReadAsync();
                        }

                        await ReadAsync().DefaultTimeout();
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
    public async Task StopTransportWhenConnectionAlreadyStoppedOnServer()
    {
        var pollRequestTcs = new TaskCompletionSource();

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var firstPoll = true;
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();
                if (request.Method == HttpMethod.Delete)
                {
                    // Simulate the server having already cleaned up the connection on the server
                    return ResponseUtils.CreateResponse(HttpStatusCode.NotFound);
                }
                else
                {
                    if (firstPoll)
                    {
                        firstPoll = false;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    }

                    await pollRequestTcs.Task;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
            });

        using (StartVerifiableLog())
        {
            using (var httpClient = new HttpClient(mockHttpHandler.Object))
            {
                var longPollingTransport = new LongPollingTransport(httpClient, loggerFactory: LoggerFactory);

                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary).DefaultTimeout();

                var stopTask = longPollingTransport.StopAsync();

                pollRequestTcs.SetResult();

                await stopTask.DefaultTimeout();
            }
        }
    }

    [Fact]
    public async Task LongPollingTransportStopsWhenSendRequestFails()
    {
        var stopped = false;
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();
                switch (request.Method.Method)
                {
                    case "DELETE":
                        stopped = true;
                        return ResponseUtils.CreateResponse(HttpStatusCode.Accepted);
                    case "GET" when stopped:
                        return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
                    case "GET":
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    case "POST":
                        return ResponseUtils.CreateResponse(HttpStatusCode.InternalServerError);
                    default:
                        throw new InvalidOperationException("Unexpected request");
                }
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);
            try
            {
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                await longPollingTransport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello World"));

                await longPollingTransport.Running.DefaultTimeout();

                var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await longPollingTransport.Input.ReadAllAsync().DefaultTimeout());
                Assert.Contains(" 500 ", exception.Message);

                Assert.True(stopped);
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
        var stopped = false;
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();
                if (request.Method == HttpMethod.Delete)
                {
                    stopped = true;
                    return ResponseUtils.CreateResponse(HttpStatusCode.Accepted);
                }
                else
                {
                    return stopped
                        ? ResponseUtils.CreateResponse(HttpStatusCode.NoContent)
                        : ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);
            try
            {
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                longPollingTransport.Output.Complete();

                await longPollingTransport.Running.DefaultTimeout();

                await longPollingTransport.Input.ReadAllAsync().DefaultTimeout();
            }
            finally
            {
                await longPollingTransport.StopAsync();
            }
        }
    }

    [Fact]
    public async Task LongPollingTransportShutsDownImmediatelyEvenIfServerDoesntCompletePoll()
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
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                longPollingTransport.Output.Complete();

                await longPollingTransport.Running.DefaultTimeout();

                await longPollingTransport.Input.ReadAllAsync().DefaultTimeout();
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
        var message1Payload = new[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };

        var requests = 0;
        var mockHttpHandler = new Mock<HttpMessageHandler>();
        var sentRequests = new List<HttpRequestMessage>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                sentRequests.Add(request);

                await Task.Yield();

                if (requests == 0)
                {
                    requests++;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }
                else if (requests == 1)
                {
                    requests++;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK, message1Payload);
                }

                return ResponseUtils.CreateResponse(HttpStatusCode.NoContent);
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);
            try
            {
                // Start the transport
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                // Wait for the transport to finish
                await longPollingTransport.Running.DefaultTimeout();

                // Pull Messages out of the channel
                var message = await longPollingTransport.Input.ReadAllAsync();

                // Check the provided request
                Assert.Equal(3, sentRequests.Count);

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
        var tcs = new TaskCompletionSource<HttpResponseMessage>();
        var firstPoll = true;

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
                else if (request.Method == HttpMethod.Get)
                {
                    // First poll completes immediately
                    if (firstPoll)
                    {
                        firstPoll = false;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    }

                    cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
                    // This is the poll task
                    return await tcs.Task;
                }
                else if (request.Method == HttpMethod.Delete)
                {
                    return ResponseUtils.CreateResponse(HttpStatusCode.Accepted);
                }
                return ResponseUtils.CreateResponse(HttpStatusCode.OK);
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);

            try
            {
                // Start the transport
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                longPollingTransport.Output.Write(Encoding.UTF8.GetBytes("Hello"));
                longPollingTransport.Output.Write(Encoding.UTF8.GetBytes("World"));
                await longPollingTransport.Output.FlushAsync();

                longPollingTransport.Output.Complete();

                await longPollingTransport.Running.DefaultTimeout();
                await longPollingTransport.Input.ReadAllAsync();

                Assert.Single(sentRequests);
                Assert.Equal(new[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d'
                    }, sentRequests[0]);
            }
            finally
            {
                await longPollingTransport.StopAsync();
            }
        }
    }

    [Fact]
    public async Task LongPollingTransportSendsDeleteAfterPollEnds()
    {
        var sentRequests = new List<byte[]>();
        var pollTcs = new TaskCompletionSource<HttpResponseMessage>();
        var deleteTcs = new TaskCompletionSource();
        var firstPoll = true;

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
                else if (request.Method == HttpMethod.Get)
                {
                    // First poll completes immediately
                    if (firstPoll)
                    {
                        firstPoll = false;
                        return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                    }

                    cancellationToken.Register(() => pollTcs.TrySetCanceled(cancellationToken));
                    // This is the poll task
                    return await pollTcs.Task;
                }
                else if (request.Method == HttpMethod.Delete)
                {
                    // The poll task should have been completed
                    Assert.True(pollTcs.Task.IsCompleted);

                    deleteTcs.TrySetResult();

                    return ResponseUtils.CreateResponse(HttpStatusCode.Accepted);
                }
                return ResponseUtils.CreateResponse(HttpStatusCode.OK);
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);

            // Start the transport
            await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

            var task = longPollingTransport.StopAsync();

            await deleteTcs.Task.DefaultTimeout();

            await task.DefaultTimeout();
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
                await longPollingTransport.StartAsync(TestUri, transferFormat);
            }
            finally
            {
                await longPollingTransport.StopAsync();
            }
        }
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
                longPollingTransport.StartAsync(TestUri, transferFormat));

            Assert.Contains($"The '{transferFormat}' transfer format is not supported by this transport.", exception.Message);
            Assert.Equal("transferFormat", exception.ParamName);
        }
    }

    [Fact]
    public async Task LongPollingTransportRePollsIfRequestCanceled()
    {
        var numPolls = 0;
        var completionTcs = new TaskCompletionSource();

        var mockHttpHandler = new Mock<HttpMessageHandler>();
        mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
            {
                await Task.Yield();

                if (numPolls == 0)
                {
                    numPolls++;
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK);
                }

                if (numPolls++ < 3)
                {
                    throw new OperationCanceledException();
                }

                completionTcs.SetResult();
                return ResponseUtils.CreateResponse(HttpStatusCode.OK);
            });

        using (var httpClient = new HttpClient(mockHttpHandler.Object))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);

            try
            {
                await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);

                var completedTask = await Task.WhenAny(completionTcs.Task, longPollingTransport.Running).DefaultTimeout();
                Assert.Equal(completionTcs.Task, completedTask);
            }
            finally
            {
                await longPollingTransport.StopAsync();
            }
        }
    }

    [Fact]
    public async Task SendsDeleteRequestWhenTransportCompleted()
    {
        var handler = TestHttpMessageHandler.CreateDefault();

        using (var httpClient = new HttpClient(handler))
        {
            var longPollingTransport = new LongPollingTransport(httpClient);

            await longPollingTransport.StartAsync(TestUri, TransferFormat.Binary);
            await longPollingTransport.StopAsync();

            var deleteRequest = handler.ReceivedRequests.SingleOrDefault(r => r.Method == HttpMethod.Delete);
            Assert.NotNull(deleteRequest);
            Assert.Equal(TestUri, deleteRequest.RequestUri);
        }
    }

    [Fact]
    public async Task PollRequestsContainCorrectAcceptHeader()
    {
        var testHttpHandler = new TestHttpMessageHandler();
        var responseTaskCompletionSource = new TaskCompletionSource<HttpResponseMessage>();
        var requestCount = 0;
        var allHeadersCorrect = true;
        var secondRequestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        testHttpHandler.OnRequest(async (request, next, cancellationToken) =>
        {
            if (request.Headers.Accept?.Contains(new MediaTypeWithQualityHeaderValue("*/*")) != true)
            {
                allHeadersCorrect = false;
            }

            requestCount++;

            if (requestCount == 2)
            {
                secondRequestReceived.SetResult();
            }

            if (requestCount >= 2)
            {
                if (allHeadersCorrect)
                {
                    responseTaskCompletionSource.TrySetResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
                else
                {
                    responseTaskCompletionSource.TrySetResult(new HttpResponseMessage(HttpStatusCode.NoContent));
                }
            }

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using (var httpClient = new HttpClient(testHttpHandler))
        {
            var loggerFactory = NullLoggerFactory.Instance;
            var transport = new LongPollingTransport(httpClient, loggerFactory: loggerFactory);

            var startTask = transport.StartAsync(TestUri, TransferFormat.Text);

            await secondRequestReceived.Task.DefaultTimeout();

            await transport.StopAsync();

            Assert.True(responseTaskCompletionSource.Task.IsCompleted);
            var response = await responseTaskCompletionSource.Task.DefaultTimeout();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
