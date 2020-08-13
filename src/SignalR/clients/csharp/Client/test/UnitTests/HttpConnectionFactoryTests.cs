// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HttpConnectionFactoryTests
    {
        [Fact]
        public async Task ConnectionIsDisposedIfItFailsToStartAsync()
        {
            var testHandler = new TestHttpMessageHandler(autoNegotiate: false, handleFirstPoll: false);
            testHandler.OnRequest((req, next, ct) => Task.FromException<HttpResponseMessage>(new Exception("BOOM")));

            var factory = new HttpConnectionFactory(
                Options.Create(new HttpConnectionOptions
                {
                    DefaultTransferFormat = TransferFormat.Text,
                    HttpMessageHandlerFactory = _ => testHandler,
                }),
                NullLoggerFactory.Instance);

            // We don't care about the specific exception
            await Assert.ThrowsAnyAsync<Exception>(async () => await factory.ConnectAsync(new UriEndPoint(new Uri("http://example.com"))));

            // We care that the handler (and by extension the client) was disposed
            Assert.True(testHandler.Disposed);
        }

        [Fact]
        public async Task DoesNotSupportNonUriEndPoints()
        {
            var factory = new HttpConnectionFactory(
                Options.Create(new HttpConnectionOptions { DefaultTransferFormat = TransferFormat.Text }),
                NullLoggerFactory.Instance);

            var ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await factory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 0)));

            Assert.Equal("The provided EndPoint must be of type UriEndPoint.", ex.Message);
        }

        [Fact]
        public async Task OptionsUrlMustMatchEndPointIfSet()
        {
            var url1 = new Uri("http://example.com/1");
            var url2 = new Uri("http://example.com/2");

            var factory = new HttpConnectionFactory(
                Options.Create(new HttpConnectionOptions
                {
                    Url = url1,
                    DefaultTransferFormat = TransferFormat.Text
                }),
                NullLoggerFactory.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await factory.ConnectAsync(new UriEndPoint(url2)));
            Assert.Equal("If HttpConnectionOptions.Url was set, it must match the UriEndPoint.Uri passed to ConnectAsync.", ex.Message);
        }

        [Fact]
        public void ShallowCopyHttpConnectionOptionsCopiesAllPublicProperties()
        {
            Func<HttpMessageHandler, HttpMessageHandler> handlerFactory = handler => handler;
            Func<Task<string>> tokenProvider = () => Task.FromResult("");
            Action<ClientWebSocketOptions> webSocketConfig = options => { };

            var testValues = new Dictionary<string, object>
            {
                { $"{nameof(HttpConnectionOptions.HttpMessageHandlerFactory)}", handlerFactory },
                { $"{nameof(HttpConnectionOptions.Headers)}", new Dictionary<string, string>() },
                { $"{nameof(HttpConnectionOptions.ClientCertificates)}", new X509CertificateCollection() },
                { $"{nameof(HttpConnectionOptions.Cookies)}", new CookieContainer() },
                { $"{nameof(HttpConnectionOptions.Url)}", new Uri("https://example.com") },
                { $"{nameof(HttpConnectionOptions.Transports)}", HttpTransportType.ServerSentEvents },
                { $"{nameof(HttpConnectionOptions.SkipNegotiation)}", true },
                { $"{nameof(HttpConnectionOptions.AccessTokenProvider)}", tokenProvider },
                { $"{nameof(HttpConnectionOptions.CloseTimeout)}", TimeSpan.FromDays(1) },
                { $"{nameof(HttpConnectionOptions.Credentials)}", Mock.Of<ICredentials>() },
                { $"{nameof(HttpConnectionOptions.Proxy)}", Mock.Of<IWebProxy>() },
                { $"{nameof(HttpConnectionOptions.UseDefaultCredentials)}", true },
                { $"{nameof(HttpConnectionOptions.DefaultTransferFormat)}", TransferFormat.Text },
                { $"{nameof(HttpConnectionOptions.WebSocketConfiguration)}", webSocketConfig },
            };

            var options = new HttpConnectionOptions();
            var properties = typeof(HttpConnectionOptions)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                property.SetValue(options, testValues[property.Name]);
            }

            var shallowCopiedOptions = HttpConnectionFactory.ShallowCopyHttpConnectionOptions(options);

            foreach (var property in properties)
            {
                Assert.Equal(testValues[property.Name], property.GetValue(shallowCopiedOptions));
                testValues.Remove(property.Name);
            }

            Assert.Empty(testValues);
        }
    }
}
