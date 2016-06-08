// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class WebApiCompatShimActionResultTest : IClassFixture<MvcTestFixture<WebApiCompatShimWebSite.Startup>>
    {
        public WebApiCompatShimActionResultTest(MvcTestFixture<WebApiCompatShimWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ApiController_BadRequest()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetBadRequest");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_BadRequestMessage()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetBadRequestMessage");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("{\"Message\":\"Hello, world!\"}", content);
        }

        [Fact]
        public async Task ApiController_BadRequestModelState()
        {
            // Arrange
            var expected = "{\"Message\":\"The request is invalid.\",\"ModelState\":{\"product.Name\":[\"Name is required.\"]}}";

            // Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetBadRequestModelState");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_Conflict()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetConflict");

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_Content()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetContent");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Ambiguous, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
        }

        [Fact]
        public async Task ApiController_CreatedRelative()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedRelative");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
            Assert.Equal("5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedAbsolute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedAbsolute");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
            Assert.Equal("/api/Blog/ActionResult/GetUser/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedQualified()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedQualified");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
            Assert.Equal("http://localhost/api/Blog/ActionResult/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedUri()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedUri");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
            Assert.Equal("/api/Blog/ActionResult/GetUser/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedAtRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedAtRoute");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
            Assert.Equal("http://localhost/api/Blog/ActionResult/GetUser/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_InternalServerError()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetInternalServerError");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_InternalServerErrorException()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetInternalServerErrorException");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("{\"Message\":\"An error has occurred.\"}", content);
        }

        [Fact]
        public async Task ApiController_Json()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetJson");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
        }

        [Fact]
        public async Task ApiController_JsonSettings()
        {
            // Arrange
            var expected =
                "{" + Environment.NewLine +
                "  \"name\": \"Test User\"" + Environment.NewLine +
                "}";

            // Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetJsonSettings");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_JsonSettingsEncoding()
        {
            // Arrange
            var expected =
                "{" + Environment.NewLine +
                "  \"name\": \"Test User\"" + Environment.NewLine +
                "}";

            // Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetJsonSettingsEncoding");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
            Assert.Equal("utf-32", response.Content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task ApiController_NotFound()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetNotFound");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_Ok()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetOk");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_OkContent()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetOkContent");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"name\":\"Test User\"}", content);
        }

        [Fact]
        public async Task ApiController_RedirectString()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetRedirectString");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("http://localhost/api/Users", response.Headers.Location.OriginalString);
        }

        [Theory]
        [InlineData("http://localhost/api/Blog/ActionResult/GetRedirectUri", "api/Blog")]
        [InlineData(
            "http://localhost/api/Blog/ActionResult/GetRedirectUrlUsingRouteName",
            "/api/Blog/BasicApi/WriteToHttpContext")]
        public async Task ApiController_RedirectUri(string url, string expected)
        {
            // Arrange & Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(expected, response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_ResponseMessage()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetResponseMessage");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new string[] { "Hello" }, response.Headers.GetValues("X-Test"));
        }

        [Fact]
        public async Task ApiController_StatusCode()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/ActionResult/GetStatusCode");

            // Assert
            Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        }
    }
}