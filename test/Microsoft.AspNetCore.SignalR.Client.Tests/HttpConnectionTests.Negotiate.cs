// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Xunit;

using TransportType = Microsoft.AspNetCore.Sockets.TransportType;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        public class Negotiate
        {
            [Theory]
            [InlineData("")]
            [InlineData("Not Json")]
            public Task StartThrowsFormatExceptionIfNegotiationResponseIsInvalid(string negotiatePayload)
            {
                return RunInvalidNegotiateResponseTest<FormatException>(negotiatePayload, "Invalid negotiation response received.");
            }

            [Fact]
            public Task StartThrowsFormatExceptionIfNegotiationResponseHasNoConnectionId()
            {
                return RunInvalidNegotiateResponseTest<FormatException>(ResponseUtils.CreateNegotiationContent(connectionId: null), "Invalid connection id.");
            }

            [Fact]
            public Task StartThrowsFormatExceptionIfNegotiationResponseHasNoTransports()
            {
                return RunInvalidNegotiateResponseTest<FormatException>(ResponseUtils.CreateNegotiationContent(transportTypes: null), "No transports returned in negotiation response.");
            }

            [Theory]
            [InlineData((TransportType)0)]
            [InlineData(TransportType.ServerSentEvents)]
            public Task ConnectionCannotBeStartedIfNoCommonTransportsBetweenClientAndServer(TransportType serverTransports)
            {
                return RunInvalidNegotiateResponseTest<InvalidOperationException>(ResponseUtils.CreateNegotiationContent(transportTypes: serverTransports), "Unable to connect to the server with any of the available transports.");
            }

            [Theory]
            [InlineData("http://fakeuri.org/", "http://fakeuri.org/negotiate")]
            [InlineData("http://fakeuri.org/?q=1/0", "http://fakeuri.org/negotiate?q=1/0")]
            [InlineData("http://fakeuri.org?q=1/0", "http://fakeuri.org/negotiate?q=1/0")]
            [InlineData("http://fakeuri.org/endpoint", "http://fakeuri.org/endpoint/negotiate")]
            [InlineData("http://fakeuri.org/endpoint/", "http://fakeuri.org/endpoint/negotiate")]
            [InlineData("http://fakeuri.org/endpoint?q=1/0", "http://fakeuri.org/endpoint/negotiate?q=1/0")]
            public async Task CorrectlyHandlesQueryStringWhenAppendingNegotiateToUrl(string requestedUrl, string expectedNegotiate)
            {
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);

                var negotiateUrlTcs = new TaskCompletionSource<string>();
                testHttpHandler.OnLongPoll(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                testHttpHandler.OnNegotiate((request, cancellationToken) =>
                {
                    negotiateUrlTcs.TrySetResult(request.RequestUri.ToString());
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        ResponseUtils.CreateNegotiationContent());
                });

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, url: requestedUrl),
                    async (connection, closed) =>
                    {
                        await connection.StartAsync().OrTimeout();
                    });

                Assert.Equal(expectedNegotiate, await negotiateUrlTcs.Task.OrTimeout());
            }

            private async Task RunInvalidNegotiateResponseTest<TException>(string negotiatePayload, string expectedExceptionMessage) where TException : Exception
            {
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);

                testHttpHandler.OnNegotiate((_, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.OK, negotiatePayload));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection, closed) =>
                    {
                        var exception = await Assert.ThrowsAsync<TException>(
                            () => connection.StartAsync().OrTimeout());

                        Assert.Equal(expectedExceptionMessage, exception.Message);
                    });
            }
        }
    }
}
