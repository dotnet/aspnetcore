// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;
using Xunit.Abstractions;

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
        [InlineData((HttpTransportType)0)]
        [InlineData(HttpTransportType.All + 1)]
        public void CannotStartConnectionWithInvalidTransportType(HttpTransportType requestedTransportType)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new HttpConnection(new Uri("http://fakeuri.org/"), requestedTransportType));
        }

        [Fact]
        public async Task HttpOptionsSetOntoHttpClientHandler()
        {
            var testHttpHandler = TestHttpMessageHandler.CreateDefault();

            var negotiateUrlTcs = new TaskCompletionSource<string>();
            testHttpHandler.OnNegotiate((request, cancellationToken) =>
            {
                negotiateUrlTcs.TrySetResult(request.RequestUri.ToString());
                return ResponseUtils.CreateResponse(HttpStatusCode.OK,
                    ResponseUtils.CreateNegotiationContent());
            });

            HttpClientHandler httpClientHandler = null;

            var httpOptions = new HttpOptions();
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
                async (connection) =>
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

        [Fact]
        public async Task HttpRequestAndErrorResponseLogged()
        {
            var testHttpHandler = new TestHttpMessageHandler(false);

            testHttpHandler.OnNegotiate((request, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.BadGateway));

            var httpOptions = new HttpOptions();
            httpOptions.HttpMessageHandler = inner => testHttpHandler;

            const string loggerName = "Microsoft.AspNetCore.Http.Connections.Client.Internal.LoggingHttpMessageHandler";
            var testSink = new TestSink();
            var logger = new TestLogger(loggerName, testSink, true);

            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory
                .Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns((string categoryName) => (categoryName == loggerName) ? (ILogger)logger : NullLogger.Instance);

            try
            {
                await WithConnectionAsync(
                    CreateConnection(httpOptions, loggerFactory: mockLoggerFactory.Object, url: "http://fakeuri.org/"),
                    async (connection) =>
                    {
                        await connection.StartAsync(TransferFormat.Text).OrTimeout();
                    });
            }
            catch
            {
                // ignore connection error
            }

            Assert.Equal(2, testSink.Writes.Count);
            Assert.Equal("SendingHttpRequest", testSink.Writes[0].EventId.Name);
            Assert.Equal("UnsuccessfulHttpResponse", testSink.Writes[1].EventId.Name);
        }
    }
}
