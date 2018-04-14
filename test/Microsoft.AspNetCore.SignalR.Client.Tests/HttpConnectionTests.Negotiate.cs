// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Moq;
using Newtonsoft.Json;
using Xunit;

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
                return RunInvalidNegotiateResponseTest<InvalidDataException>(negotiatePayload, "Invalid negotiation response received.");
            }

            [Fact]
            public Task StartThrowsFormatExceptionIfNegotiationResponseHasNoConnectionId()
            {
                return RunInvalidNegotiateResponseTest<FormatException>(ResponseUtils.CreateNegotiationContent(connectionId: string.Empty), "Invalid connection id.");
            }

            [Fact]
            public Task StartThrowsFormatExceptionIfNegotiationResponseHasNoTransports()
            {
                return RunInvalidNegotiateResponseTest<InvalidOperationException>(ResponseUtils.CreateNegotiationContent(transportTypes: 0), "Unable to connect to the server with any of the available transports.");
            }

            [Theory]
            [InlineData(HttpTransportType.None)]
            [InlineData(HttpTransportType.ServerSentEvents)]
            public Task ConnectionCannotBeStartedIfNoCommonTransportsBetweenClientAndServer(HttpTransportType serverTransports)
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
                testHttpHandler.OnLongPollDelete(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                testHttpHandler.OnNegotiate((request, cancellationToken) =>
                {
                    negotiateUrlTcs.TrySetResult(request.RequestUri.ToString());
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        ResponseUtils.CreateNegotiationContent());
                });

                using (var noErrorScope = new VerifyNoErrorsScope())
                { 
                    await WithConnectionAsync(
                        CreateConnection(testHttpHandler, url: requestedUrl, loggerFactory: noErrorScope.LoggerFactory),
                        async (connection) =>
                        {
                            await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        });
                }

                Assert.Equal(expectedNegotiate, await negotiateUrlTcs.Task.OrTimeout());
            }

            [Fact]
            public async Task StartSkipsOverTransportsThatTheClientDoesNotUnderstand()
            {
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);

                testHttpHandler.OnLongPoll(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                testHttpHandler.OnNegotiate((request, cancellationToken) =>
                {
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        JsonConvert.SerializeObject(new
                        {
                            connectionId = "00000000-0000-0000-0000-000000000000",
                            availableTransports = new object[]
                            {
                                new
                                {
                                    transport = "QuantumEntanglement",
                                    transferFormats = new[] { "Qbits" },
                                },
                                new
                                {
                                    transport = "CarrierPigeon",
                                    transferFormats = new[] { "Text" },
                                },
                                new
                                {
                                    transport = "LongPolling",
                                    transferFormats = new[] { "Text", "Binary" }
                                },
                            }
                        }));
                });

                var transportFactory = new Mock<ITransportFactory>(MockBehavior.Strict);

                transportFactory.Setup(t => t.CreateTransport(HttpTransportType.LongPolling))
                    .Returns(new TestTransport(transferFormat: TransferFormat.Text | TransferFormat.Binary));

                using (var noErrorScope = new VerifyNoErrorsScope())
                {
                    await WithConnectionAsync(
                        CreateConnection(testHttpHandler, transportFactory: transportFactory.Object, loggerFactory: noErrorScope.LoggerFactory),
                        async (connection) =>
                        {
                            await connection.StartAsync(TransferFormat.Binary).OrTimeout();
                        });
                }
            }

            [Fact]
            public async Task StartSkipsOverTransportsThatDoNotSupportTheRequredTransferFormat()
            {
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);

                testHttpHandler.OnLongPoll(cancellationToken => ResponseUtils.CreateResponse(HttpStatusCode.NoContent));
                testHttpHandler.OnNegotiate((request, cancellationToken) =>
                {
                    return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                        JsonConvert.SerializeObject(new
                        {
                            connectionId = "00000000-0000-0000-0000-000000000000",
                            availableTransports = new object[]
                            {
                                new
                                {
                                    transport = "WebSockets",
                                    transferFormats = new[] { "Qbits" },
                                },
                                new
                                {
                                    transport = "ServerSentEvents",
                                    transferFormats = new[] { "Text" },
                                },
                                new
                                {
                                    transport = "LongPolling",
                                    transferFormats = new[] { "Text", "Binary" }
                                },
                            }
                        }));
                });

                var transportFactory = new Mock<ITransportFactory>(MockBehavior.Strict);

                transportFactory.Setup(t => t.CreateTransport(HttpTransportType.LongPolling))
                    .Returns(new TestTransport(transferFormat: TransferFormat.Text | TransferFormat.Binary));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler, transportFactory: transportFactory.Object),
                    async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Binary).OrTimeout();
                    });
            }

            private async Task RunInvalidNegotiateResponseTest<TException>(string negotiatePayload, string expectedExceptionMessage) where TException : Exception
            {
                var testHttpHandler = new TestHttpMessageHandler(autoNegotiate: false);

                testHttpHandler.OnNegotiate((_, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.OK, negotiatePayload));

                await WithConnectionAsync(
                    CreateConnection(testHttpHandler),
                    async (connection) =>
                    {
                        var exception = await Assert.ThrowsAsync<TException>(
                            () => connection.StartAsync(TransferFormat.Text).OrTimeout());

                        Assert.Equal(expectedExceptionMessage, exception.Message);
                    });
            }
        }
    }
}
