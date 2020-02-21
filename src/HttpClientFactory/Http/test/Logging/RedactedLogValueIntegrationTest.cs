// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Logging
{
    public class RedactedLogValueIntegrationTest
    {
        [Fact]
        public async Task RedactHeaderValueWithHeaderList_ValueIsRedactedBeforeLogging()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            // Act
            serviceCollection
                .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler())
                .RedactLoggedHeaders(new[] { "Authorization", "X-Sensitive", });

            // Assert
            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            request.Headers.Authorization = new AuthenticationHeaderValue("fake", "secret value");
            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true, };

            await client.SendAsync(request);

            var messages = sink.Writes.ToArray();

            var message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.RequestHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
            }));
            Assert.Equal(
@"Request Headers:
Authorization: *
Cache-Control: no-cache
", message.Message);

            message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
            }));
            Assert.Equal(
@"Request Headers:
Authorization: *
Cache-Control: no-cache
", message.Message);

            message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingHttpMessageHandler.Log.EventIds.ResponseHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
            }));
            Assert.Equal(
@"Response Headers:
X-Sensitive: *
Y-Non-Sensitive: innocuous value
", message.Message);

            message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.ResponseHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
            }));
            Assert.Equal(
@"Response Headers:
X-Sensitive: *
Y-Non-Sensitive: innocuous value
", message.Message);
        }

        [Fact]
        public async Task RedactHeaderValueWithPredicate_ValueIsRedactedBeforeLogging()
        {
            // Arrange
            var sink = new TestSink();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILoggerFactory>(new TestLoggerFactory(sink, enabled: true));

            // Act
            serviceCollection
                .AddHttpClient("test")
                .ConfigurePrimaryHttpMessageHandler(() => new TestMessageHandler())
                .RedactLoggedHeaders(header =>
                {
                    return header.StartsWith("Auth") || header.StartsWith("X-");
                });

            // Assert
            var services = serviceCollection.BuildServiceProvider();

            var client = services.GetRequiredService<IHttpClientFactory>().CreateClient("test");

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            request.Headers.Authorization = new AuthenticationHeaderValue("fake", "secret value");
            request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true, };

            await client.SendAsync(request);

            var messages = sink.Writes.ToArray();

            var message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.RequestHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
            }));
            Assert.Equal(
@"Request Headers:
Authorization: *
Cache-Control: no-cache
", message.Message);

            message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingHttpMessageHandler.Log.EventIds.RequestHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
            }));
            Assert.Equal(
@"Request Headers:
Authorization: *
Cache-Control: no-cache
", message.Message);

            message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingHttpMessageHandler.Log.EventIds.ResponseHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.ClientHandler";
            }));
            Assert.Equal(
@"Response Headers:
X-Sensitive: *
Y-Non-Sensitive: innocuous value
", message.Message);

            message = Assert.Single(messages.Where(m =>
            {
                return
                    m.EventId == LoggingScopeHttpMessageHandler.Log.EventIds.ResponseHeader &&
                    m.LoggerName == "System.Net.Http.HttpClient.test.LogicalHandler";
            }));
            Assert.Equal(
@"Response Headers:
X-Sensitive: *
Y-Non-Sensitive: innocuous value
", message.Message);
        }

        private class TestMessageHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage();
                response.Headers.Add("X-Sensitive", "secret value");
                response.Headers.Add("Y-Non-Sensitive", "innocuous value");

                return Task.FromResult(response);
            }
        }
    }
}
