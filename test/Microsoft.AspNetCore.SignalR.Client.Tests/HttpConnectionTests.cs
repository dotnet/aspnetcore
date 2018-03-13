// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;

// This is needed because there's a System.Net.TransportType in net461 (it's internal in netcoreapp).
using TransportType = Microsoft.AspNetCore.Sockets.TransportType;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests : LoggedTest
    {
        public HttpConnectionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CannotCreateConnectionWithNullUrl()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new HttpConnection(null));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public void ConnectionReturnsUrlUsedToStartTheConnection()
        {
            var connectionUrl = new Uri("http://fakeuri.org/");
            Assert.Equal(connectionUrl, new HttpConnection(connectionUrl).Url);
        }

        [Theory]
        [InlineData((TransportType)0)]
        [InlineData(TransportType.All + 1)]
        public void CannotStartConnectionWithInvalidTransportType(TransportType requestedTransportType)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HttpConnection(new Uri("http://fakeuri.org/"), requestedTransportType));
        }

        [Fact]
        public async Task EventsAreNotRunningOnMainLoop()
        {
            var testTransport = new TestTransport();

            await WithConnectionAsync(
                CreateConnection(transport: testTransport),
                async (connection, closed) =>
                {
                    // Block up the OnReceived callback until we finish the test.
                    var onReceived = new SyncPoint();
                    connection.OnReceived(_ => onReceived.WaitToContinue().OrTimeout());

                    await connection.StartAsync(TransferFormat.Text).OrTimeout();

                    // This will trigger the received callback
                    await testTransport.Application.Output.WriteAsync(new byte[] { 1 });

                    // Wait to hit the sync point. We are now blocking up the TaskQueue
                    await onReceived.WaitForSyncPoint().OrTimeout();

                    // Now we write something else and we want to test that the HttpConnection receive loop is still
                    // removing items from the channel even though OnReceived is blocked up.
                    await testTransport.Application.Output.WriteAsync(new byte[] { 1 });

                    // Now that we've written, we wait for WaitToReadAsync to return an INCOMPLETE task. It will do so
                    // once HttpConnection reads the message. We also use a CTS to timeout in case the loop is indeed blocked
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5));
                    while (testTransport.Application.Input.WaitToReadAsync().IsCompleted && !cts.IsCancellationRequested)
                    {
                        // Yield to allow the HttpConnection to dequeue the message
                        await Task.Yield();
                    }

                    // If we exited because we were cancelled, throw.
                    cts.Token.ThrowIfCancellationRequested();

                    // We're free! Unblock onreceived
                    onReceived.Continue();
                });
        }

        [Fact]
        public async Task EventQueueTimeout()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger<HttpConnectionTests>();

                var testTransport = new TestTransport();

                await WithConnectionAsync(
                    CreateConnection(transport: testTransport),
                    async (connection, closed) =>
                    {
                        var onReceived = new SyncPoint();
                        connection.OnReceived(_ => onReceived.WaitToContinue().OrTimeout());

                        logger.LogInformation("Starting connection");
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                        logger.LogInformation("Started connection");

                        await testTransport.Application.Output.WriteAsync(new byte[] { 1 });
                        await onReceived.WaitForSyncPoint().OrTimeout();

                        // Dispose should complete, even though the receive callbacks are completely blocked up.
                        logger.LogInformation("Disposing connection");
                        await connection.DisposeAsync().OrTimeout(TimeSpan.FromSeconds(10));
                        logger.LogInformation("Disposed connection");

                        // Clear up blocked tasks.
                        onReceived.Continue();
                    });
            }
        }

        [Fact]
        public async Task HttpOptionsSetOntoHttpClientHandler()
        {
            var testHttpHandler = new TestHttpMessageHandler();

            var negotiateUrlTcs = new TaskCompletionSource<string>();
            testHttpHandler.OnNegotiate((request, cancellationToken) =>
            {
                negotiateUrlTcs.TrySetResult(request.RequestUri.ToString());
                return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                    ResponseUtils.CreateNegotiationContent());
            });

            HttpClientHandler httpClientHandler = null;

            HttpOptions httpOptions = new HttpOptions();
            httpOptions.HttpMessageHandler = inner =>
            {
                httpClientHandler = (HttpClientHandler)inner;
                return testHttpHandler;
            };
            httpOptions.Cookies.Add(new Cookie("Name", "Value", string.Empty, "fakeuri.org"));
            var clientCertificate = new X509Certificate();
            httpOptions.ClientCertificates.Add(clientCertificate);
            httpOptions.UseDefaultCredentials = false;
            httpOptions.Credentials = Mock.Of<ICredentials>();
            httpOptions.Proxy = Mock.Of<IWebProxy>();

            await WithConnectionAsync(
                CreateConnection(httpOptions, url: "http://fakeuri.org/"),
                async (connection, closed) =>
                {
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();
                });

            Assert.NotNull(httpClientHandler);
            Assert.Equal(1, httpClientHandler.CookieContainer.Count);
            Assert.Single(httpClientHandler.ClientCertificates);
            Assert.Same(clientCertificate, httpClientHandler.ClientCertificates[0]);
            Assert.False(httpClientHandler.UseDefaultCredentials);
            Assert.Same(httpOptions.Proxy, httpClientHandler.Proxy);
            Assert.Same(httpOptions.Credentials, httpClientHandler.Credentials);
        }
    }
}
