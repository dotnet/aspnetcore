// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Cors.Core;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CorsMiddlewareTests
    {
        private const string SiteName = nameof(CorsMiddlewareWebSite);
        private readonly Action<IApplicationBuilder> _app = new CorsMiddlewareWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new CorsMiddlewareWebSite.Startup().ConfigureServices;

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        public async Task ResourceWithSimpleRequestPolicy_Allows_SimpleRequests(string method)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var origin = "http://example.com";

            var requestBuilder = server
                .CreateRequest("http://localhost/CorsMiddleware/GetExclusiveContent")
                .AddHeader(CorsConstants.Origin, origin);

            // Act
            var response = await requestBuilder.SendAsync(method);

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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Adding a custom header makes it a non simple request.
            var requestBuilder = server
                .CreateRequest("http://localhost/CorsMiddleware/GetExclusiveContent")
                .AddHeader(CorsConstants.Origin, "http://example.com")
                .AddHeader(CorsConstants.AccessControlRequestMethod, method)
                .AddHeader(CorsConstants.AccessControlRequestHeaders, "Custom");

            // Act
            var response = await requestBuilder.SendAsync(CorsConstants.PreflightHttpMethod);

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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Adding a custom header makes it a non simple request.
            var requestBuilder = server
                .CreateRequest("http://localhost/CorsMiddleware/GetExclusiveContent")
                .AddHeader(CorsConstants.Origin, "http://example2.com");

            // Act
            var response = await requestBuilder.SendAsync("PUT");

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