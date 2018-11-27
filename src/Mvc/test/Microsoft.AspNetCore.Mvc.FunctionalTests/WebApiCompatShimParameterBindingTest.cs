// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class WebApiCompatShimParameterBindingTest : IClassFixture<MvcTestFixture<WebApiCompatShimWebSite.Startup>>
    {
        public WebApiCompatShimParameterBindingTest(MvcTestFixture<WebApiCompatShimWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("http://localhost/api/Blog/Employees/PostByIdDefault/5")]
        [InlineData("http://localhost/api/Blog/Employees/PostByIdDefault?id=5")]
        public async Task ApiController_SimpleParameter_Default_ReadsFromUrl(string url)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("5", content);
        }

        [Fact]
        public async Task ApiController_SimpleParameter_Default_DoesNotReadFormData()
        {
            // Arrange
            var url = "http://localhost/api/Blog/Employees/PostByIdDefault";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "id", "5" },
            });

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("-1", content);
        }

        [Theory]
        [InlineData("http://localhost/api/Blog/Employees/PostByIdModelBinder/5")]
        [InlineData("http://localhost/api/Blog/Employees/PostByIdModelBinder?id=5")]
        public async Task ApiController_SimpleParameter_ModelBinder_ReadsFromUrl(string url)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("5", content);
        }

        [Fact]
        public async Task ApiController_SimpleParameter_ModelBinder_ReadsFromFormData()
        {
            // Arrange
            var url = "http://localhost/api/Blog/Employees/PostByIdModelBinder";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "id", "5" },
            });

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("5", content);
        }

        [Theory]
        [InlineData("http://localhost/api/Blog/Employees/PostByIdFromQuery/5", "-1")]
        [InlineData("http://localhost/api/Blog/Employees/PostByIdFromQuery?id=5", "5")]
        public async Task ApiController_SimpleParameter_FromQuery_ReadsFromQueryNotRouteData(string url, string expected)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_SimpleParameter_FromQuery_DoesNotReadFormData()
        {
            // Arrange
            var url = "http://localhost/api/Blog/Employees/PostByIdFromQuery";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "id", "5" },
            });

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("-1", content);
        }

        [Fact]
        public async Task ApiController_ComplexParameter_Default_ReadsFromBody()
        {
            // Arrange
            var url = "http://localhost/api/Blog/Employees/PutEmployeeDefault";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                Id = 5,
                Name = "Test Employee",
            }));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"id\":5,\"name\":\"Test Employee\"}", content);
        }

        [Fact]
        public async Task ApiController_ComplexParameter_ModelBinder_ReadsFormAndUrl()
        {
            // Arrange
            var url = "http://localhost/api/Blog/Employees/PutEmployeeModelBinder/5";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "name", "Test Employee" },
            });

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"id\":5,\"name\":\"Test Employee\"}", content);
        }

        // name is read from the url - and the rest from the body (formatters)
        [Fact]
        public async Task ApiController_TwoParameters_DefaultSources()
        {
            // Arrange
            var url = "http://localhost/api/Blog/Employees/PutEmployeeBothDefault?name=Name_Override";
            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Content = new StringContent(JsonConvert.SerializeObject(new
            {
                Id = 5,
                Name = "Test Employee",
            }));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            // Act
            var response = await Client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"id\":5,\"name\":\"Name_Override\"}", content);
        }
    }
}
