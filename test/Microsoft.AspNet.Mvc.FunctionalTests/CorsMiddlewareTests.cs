// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CorsMiddlewareTests : IClassFixture<MvcTestFixture<CorsMiddlewareWebSite.Startup>>
    {
        public CorsMiddlewareTests(MvcTestFixture<CorsMiddlewareWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        public async Task ResourceWithSimpleRequestPolicy_Allows_SimpleRequests(string method)
        {
            // Arrange
            var origin = "http://example.com";
            var request = new HttpRequestMessage(
                new HttpMethod(method),
                "http://localhost/CorsMiddleware/GetExclusiveContent");
            request.Headers.Add(CorsConstants.Origin, origin);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("exclusive", content);
            var responseHeaders = response.Headers;
            var header = Assert.Single(response.Headers);
            Assert.Equal(CorsConstants.AccessControlAllowOrigin, header.Key);
            Assert.Equal(new[] { "http://example.com" }, header.Value.ToArray());
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task PolicyFailed_Disallows_PreFlightRequest(string method)
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod(CorsConstants.PreflightHttpMethod),
                "http://localhost/CorsMiddleware/GetExclusiveContent");

            // Adding a custom header makes it a non-simple request.
            request.Headers.Add(CorsConstants.Origin, "http://example.com");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, method);
            request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            // Middleware applied the policy and since that did not pass, there were no access control headers.
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(response.Headers);

            // It should short circuit and hence no result.
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public async Task PolicyFailed_Allows_ActualRequest_WithMissingResponseHeaders()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Put, "http://localhost/CorsMiddleware/GetExclusiveContent");

            // Adding a custom header makes it a non simple request.
            request.Headers.Add(CorsConstants.Origin, "http://example2.com");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            // Middleware applied the policy and since that did not pass, there were no access control headers.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            // It still has executed the action.
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("exclusive", content);
        }
    }
}