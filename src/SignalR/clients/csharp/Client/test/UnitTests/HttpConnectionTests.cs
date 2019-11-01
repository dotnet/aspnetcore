// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests : VerifiableLoggedTest
    {
        [Fact]
        public void CannotCreateConnectionWithNullUrl()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new HttpConnection(null));
            Assert.Equal("url", exception.ParamName);
        }

        [Fact]
        public void CannotCreateConnectionWithNullUrlOnOptions()
        {
            var exception = Assert.Throws<ArgumentException>(() => new HttpConnection(new HttpConnectionOptions(), NullLoggerFactory.Instance));
            Assert.Equal("httpConnectionOptions", exception.ParamName);
        }

        [Fact]
        public void CannotSetConnectionId()
        {
            var connection = new HttpConnection(new Uri("http://fakeuri.org/"));
            var exception = Assert.Throws<InvalidOperationException>(() => connection.ConnectionId = "custom conneciton ID");
            Assert.Equal("The ConnectionId is set internally and should not be set by user code.", exception.Message);
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

            var httpOptions = new HttpConnectionOptions();
            httpOptions.Url = new Uri("http://fakeuri.org/");
            httpOptions.HttpMessageHandlerFactory = inner =>
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
            httpOptions.Transports = HttpTransportType.LongPolling;

            await WithConnectionAsync(
                CreateConnection(httpOptions),
                async (connection) =>
                {
                    await connection.StartAsync().OrTimeout();
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
        public void HttpOptionsCannotSetNullCookieContainer()
        {
            var httpOptions = new HttpConnectionOptions();
            Assert.NotNull(httpOptions.Cookies);
            Assert.Throws<ArgumentNullException>(() => httpOptions.Cookies = null);
        }

        [Fact]
        public async Task HttpRequestAndErrorResponseLogged()
        {
            var testHttpHandler = new TestHttpMessageHandler(false);

            testHttpHandler.OnNegotiate((request, cancellationToken) => ResponseUtils.CreateResponse(HttpStatusCode.BadGateway));

            var httpOptions = new HttpConnectionOptions();
            httpOptions.Url = new Uri("http://fakeuri.org/");
            httpOptions.HttpMessageHandlerFactory = inner => testHttpHandler;

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
                    CreateConnection(httpOptions, loggerFactory: mockLoggerFactory.Object),
                    async (connection) =>
                    {
                        await connection.StartAsync().OrTimeout();
                    });
            }
            catch
            {
                // ignore connection error
            }

            var writeList = testSink.Writes.ToList();

            Assert.Equal(2, writeList.Count);
            Assert.Equal("SendingHttpRequest", writeList[0].EventId.Name);
            Assert.Equal("UnsuccessfulHttpResponse", writeList[1].EventId.Name);
        }
    }
}
