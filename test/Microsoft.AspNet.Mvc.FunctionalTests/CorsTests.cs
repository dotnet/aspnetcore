// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Http;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CorsTests
    {
        private const string SiteName = nameof(CorsWebSite);
        private readonly Action<IApplicationBuilder> _app = new CorsWebSite.Startup().Configure;

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        public async Task ResourceWithSimpleRequestPolicy_Allows_SimpleRequests(string method)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var origin = "http://example.com";

            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/GetBlogComments")
                .AddHeader(CorsConstants.Origin, origin);

            // Act
            var response = await requestBuilder.SendAsync(method);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("[\"comment1\",\"comment2\",\"comment3\"]", content);
            var responseHeaders = response.Headers;
            var header = Assert.Single(response.Headers);
            Assert.Equal(CorsConstants.AccessControlAllowOrigin, header.Key);
            Assert.Equal(new[] { "*" }, header.Value.ToArray());
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task PolicyFailed_Disallows_PreFlightRequest(string method)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Adding a custom header makes it a non simple request.
            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/GetBlogComments")
                .AddHeader(CorsConstants.Origin, "http://example.com")
                .AddHeader(CorsConstants.AccessControlRequestMethod, method)
                .AddHeader(CorsConstants.AccessControlRequestHeaders, "Custom");

            // Act
            var response = await requestBuilder.SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            // MVC applied the policy and since that did not pass, there were no access control headers.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            // It should short circuit and hence no result.
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public async Task SuccessfulCorsRequest_AllowsCredentials_IfThePolicyAllowsCredentials()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Adding a custom header makes it a non simple request.
            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/EditUserComment?userComment=abcd")
                .AddHeader(CorsConstants.Origin, "http://example.com")
                .AddHeader(CorsConstants.AccessControlExposeHeaders, "exposed1,exposed2");

            // Act
            var response = await requestBuilder.SendAsync("PUT");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseHeaders = response.Headers;
            Assert.Equal(
                new[] { "http://example.com" },
                responseHeaders.GetValues(CorsConstants.AccessControlAllowOrigin).ToArray());
            Assert.Equal(
               new[] { "true" },
               responseHeaders.GetValues(CorsConstants.AccessControlAllowCredentials).ToArray());
            Assert.Equal(
               new[] { "exposed1", "exposed2" },
               responseHeaders.GetValues(CorsConstants.AccessControlExposeHeaders).ToArray());

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("abcd", content);
        }

        [Fact]
        public async Task SuccessfulPreflightRequest_AllowsCredentials_IfThePolicyAllowsCredentials()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Adding a custom header makes it a non simple request.
            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/EditUserComment?userComment=abcd")
                .AddHeader(CorsConstants.Origin, "http://example.com")
                .AddHeader(CorsConstants.AccessControlRequestMethod, "PUT")
                .AddHeader(CorsConstants.AccessControlRequestHeaders, "header1,header2");

            // Act
            var response = await requestBuilder.SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseHeaders = response.Headers;
            Assert.Equal(
                new[] { "http://example.com" },
                responseHeaders.GetValues(CorsConstants.AccessControlAllowOrigin).ToArray());
            Assert.Equal(
               new[] { "true" },
               responseHeaders.GetValues(CorsConstants.AccessControlAllowCredentials).ToArray());
            Assert.Equal(
               new[] { "header1", "header2" },
               responseHeaders.GetValues(CorsConstants.AccessControlAllowHeaders).ToArray());
            Assert.Equal(
               new[] { "PUT" },
               responseHeaders.GetValues(CorsConstants.AccessControlAllowMethods).ToArray());

            var content = await response.Content.ReadAsStringAsync();
            Assert.Empty(content);
        }

        [Fact]
        public async Task PolicyFailed_Allows_ActualRequest_WithMissingResponseHeaders()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Adding a custom header makes it a non simple request.
            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/GetUserComments")
                .AddHeader(CorsConstants.Origin, "http://example2.com");

            // Act
            var response = await requestBuilder.SendAsync("PUT");

            // Assert
            // MVC applied the policy and since that did not pass, there were no access control headers.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            // It still have executed the action.
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("[\"usercomment1\",\"usercomment2\",\"usercomment3\"]", content);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        public async Task DisableCors_ActionsCanOverride_ControllerLevel(string method)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Exclusive content is not available on other sites.
            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/GetExclusiveContent")
                .AddHeader(CorsConstants.Origin, "http://example.com");

            // Act
            var response = await requestBuilder.SendAsync(method);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Since there are no response headers, the client should step in to block the content.
            Assert.Empty(response.Headers);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("exclusive", content);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("POST")]
        public async Task DisableCors_PreFlight_ActionsCanOverride_ControllerLevel(string method)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Exclusive content is not available on other sites.
            var requestBuilder = server
                .CreateRequest("http://localhost/Cors/GetExclusiveContent")
                .AddHeader(CorsConstants.Origin, "http://example.com")
                .AddHeader(CorsConstants.AccessControlRequestMethod, method)
                .AddHeader(CorsConstants.AccessControlRequestHeaders, "Custom");

            // Act
            var response = await requestBuilder.SendAsync(CorsConstants.PreflightHttpMethod);

            // Assert
            // Since there are no response headers, the client should step in to block the content.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(response.Headers);

            // Nothing gets executed for a pre-flight request.
            var content = await response.Content.ReadAsStringAsync();
            Assert.Empty(content);
        }
    }
}