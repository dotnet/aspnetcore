// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RazorPagesTest : IClassFixture<MvcTestFixture<RazorPagesWebSite.Startup>>
    {
        public RazorPagesTest(MvcTestFixture<RazorPagesWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task HelloWorld_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorld");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, World!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithRoute_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithRoute/Some/Path/route");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, route!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithHandler_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithHandler?message=handler");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, handler!", content.Trim());
        }

        [Fact]
        public async Task HelloWorldWithPageModelHandler_CanGetContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/HelloWorldWithPageModelHandler?message=pagemodel");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello, pagemodel!", content.Trim());
        }
    }
}
