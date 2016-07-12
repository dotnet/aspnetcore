// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Routing.FunctionalTests
{
    public class RoutingSampleTest : IClassFixture<RoutingTestFixture<RoutingSample.Web.Startup>>
    {
        public RoutingSampleTest(RoutingTestFixture<RoutingSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task Routing_CanRouteRequestDelegate_ToSpecificHttpVerb()
        {
            // Arrange
            var message = new HttpRequestMessage(HttpMethod.Get, "api/get/5");

            // Act
            var response = await Client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"API Get 5", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Routing_CanRouteRequest_ToSpecificMiddleware()
        {
            // Arrange
            var message = new HttpRequestMessage(HttpMethod.Get, "api/middleware");

            // Act
            var response = await Client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($"Middleware!", await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public async Task Routing_CanRouteRequest_ToDefaultHandler(string httpVerb)
        {
            // Arrange
            var message = new HttpRequestMessage(new HttpMethod(httpVerb), "api/all/Joe/Duf");
            var expectedBody = $"Verb =  {httpVerb} - Path = /api/all/Joe/Duf - Route values - [name, Joe], [lastName, Duf]";

            // Act
            var response = await Client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }
    }
}
