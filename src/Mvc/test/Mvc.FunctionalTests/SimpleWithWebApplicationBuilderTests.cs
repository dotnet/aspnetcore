// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class SimpleWithWebApplicationBuilderTests : IClassFixture<MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup>>
    {
        public SimpleWithWebApplicationBuilderTests(MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task HelloWorld()
        {
            // Arrange
            var expected = "Hello World";

            // Act
            var content = await Client.GetStringAsync("http://localhost/");

            // Assert
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task JsonResult_Works()
        {
            // Arrange
            var expected = "{\"name\":\"John\",\"age\":42}";

            // Act
            var response = await Client.GetAsync("/json");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task OkObjectResult_Works()
        {
            // Arrange
            var expected = "{\"name\":\"John\",\"age\":42}";

            // Act
            var response = await Client.GetAsync("/ok-object");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task AcceptedObjectResult_Works()
        {
            // Arrange
            var expected = "{\"name\":\"John\",\"age\":42}";

            // Act
            var response = await Client.GetAsync("/accepted-object");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.Accepted);
            Assert.Equal("/ok-object", response.Headers.Location.ToString());
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ActionReturningMoreThanOneResult_NotFound()
        {
            // Act
            var response = await Client.GetAsync("/many-results?id=-1");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ActionReturningMoreThanOneResult_Found()
        {
            // Act
            var response = await Client.GetAsync("/many-results?id=7");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.MovedPermanently);
            Assert.Equal("/json", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task ActionReturningProblemDetails_ConfiguresContentType()
        {
            // Act
            var response = await Client.GetAsync("/problem");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.InternalServerError);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType.ToString());
        }
    }
}
