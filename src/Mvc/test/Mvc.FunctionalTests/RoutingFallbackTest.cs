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
    public class RoutingFallbackTest : IClassFixture<MvcTestFixture<RoutingWebSite.StartupForFallback>>
    {
        public RoutingFallbackTest(MvcTestFixture<RoutingWebSite.StartupForFallback> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupForFallback>();

        public HttpClient Client { get; }

        [Fact]
        public async Task Fallback_CanGet404ForMissingFile()
        {
            // Arrange
            var url = "http://localhost/pranav.jpg";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Fallback_CanAccessKnownEndpoint()
        {
            // Arrange
            var url = "http://localhost/Edit/17";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from Edit page", content.Trim());
        }

        [Fact]
        public async Task Fallback_CanFallbackToControllerInArea()
        {
            // Arrange
            var url = "http://localhost/Admin/Foo";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from fallback controller: /link_generation/Admin/Fallback/Index", content);
        }

        [Fact]
        public async Task Fallback_CanFallbackToControllerInArea_WithActionConstraints()
        {
            // Arrange
            var url = "http://localhost/Admin/Foo";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from fallback controller POST: /link_generation/Admin/Fallback/Index", content);
        }

        [Fact]
        public async Task Fallback_CanFallbackToControllerInAreaPost()
        {
            // Arrange
            var url = "http://localhost/Admin/Foo";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from fallback controller POST: /link_generation/Admin/Fallback/Index", content);
        }

        [Fact]
        public async Task Fallback_CanFallbackToPage()
        {
            // Arrange
            var url = "http://localhost/Foo";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from fallback page: /FallbackPage", content);
        }
    }
}
