// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RoutingDynamicTest : IClassFixture<MvcTestFixture<RoutingWebSite.StartupForDynamic>>
    {
        public RoutingDynamicTest(MvcTestFixture<RoutingWebSite.StartupForDynamic> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupForDynamic>();

        public HttpClient Client { get; }

        [Fact]
        public async Task DynamicController_CanGet404ForMissingAction()
        {
            // Arrange
            var url = "http://localhost/dynamic/controller%3DFake,action%3DIndex";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DynamicPage_CanGet404ForMissingAction()
        {
            // Arrange
            var url = "http://localhost/dynamicpage/page%3D%2FFake";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DynamicController_CanSelectControllerInArea()
        {
            // Arrange
            var url = "http://localhost/dynamic/area%3Dadmin,controller%3Ddynamic,action%3Dindex";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from dynamic controller: /link_generation/dynamic/index", content);
        }

        [Fact]
        public async Task DynamicController_CanSelectControllerInArea_WithActionConstraints()
        {
            // Arrange
            var url = "http://localhost/dynamic/area%3Dadmin,controller%3Ddynamic,action%3Dindex";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from dynamic controller POST: /link_generation/dynamic/index", content);
        }

        [Fact]
        public async Task DynamicPage_CanSelectPage()
        {
            // Arrange
            var url = "http://localhost/dynamicpage/page%3D%2FDynamicPage";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from dynamic page: /DynamicPage", content);
        }
    }
}
