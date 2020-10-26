// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class JsonResultWithSystemTextJsonTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithSystemTextJson>>
    {
        private IServiceCollection _serviceCollection;

        public JsonResultWithSystemTextJsonTest(MvcTestFixture<BasicWebSite.StartupWithSystemTextJson> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(b => b.UseStartup<BasicWebSite.StartupWithSystemTextJson>());
            factory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(serviceCollection => _serviceCollection = serviceCollection));

            Client = factory.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task JsonResult_UsesDefaultContentType()
        {
            // Arrange
            var url = "http://localhost/JsonResultWithSystemTextJson/Plain";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("{\"message\":\"hello\"}", content);
        }

        // Using an Accept header can't force Json to not be Json. If your accept header doesn't jive with the
        // formatters/content-type configured on the result it will be ignored.
        [Theory]
        [InlineData("application/xml")]
        [InlineData("text/xml")]
        public async Task JsonResult_Conneg_Fails(string mediaType)
        {
            // Arrange
            var url = "http://localhost/JsonResultWithSystemTextJson/Plain";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", mediaType);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("{\"message\":\"hello\"}", content);
        }

        // If the object is null, it will get formatted as JSON. NOT as a 204/NoContent
        [Fact]
        public async Task JsonResult_Null()
        {
            // Arrange
            var url = "http://localhost/JsonResultWithSystemTextJson/Null";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("null", content);
        }

        // If the object is a string, it will get formatted as JSON. NOT as text/plain.
        [Fact]
        public async Task JsonResult_String()
        {
            // Arrange
            var url = "http://localhost/JsonResultWithSystemTextJson/String";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("\"hello\"", content);
        }

        [Fact]
        public async Task JsonResult_Uses_CustomSerializerSettings()
        {
            // Arrange
            var url = "http://localhost/JsonResultWithSystemTextJson/CustomSerializerSettings";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"Message\":\"hello\"}", content);
        }

        [Fact]
        public async Task JsonResult_CustomContentType()
        {
            // Arrange
            var url = "http://localhost/JsonResultWithSystemTextJson/CustomContentType";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/message+json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("{\"message\":\"hello\"}", content);
        }
    }
}
