// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class WebApiCompatShimActionResultTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(nameof(WebApiCompatShimWebSite));
        private readonly Action<IApplicationBuilder> _app = new WebApiCompatShimWebSite.Startup().Configure;

        [Fact]
        public async Task ApiController_BadRequest()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetBadRequest");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_BadRequestMessage()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetBadRequestMessage");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("{\"Message\":\"Hello, world!\"}", content);
        }

        [Fact]
        public async Task ApiController_BadRequestModelState()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected = "{\"Message\":\"The request is invalid.\",\"ModelState\":{\"product.Name\":[\"Name is required.\"]}}";

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetBadRequestModelState");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_Conflict()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetConflict");

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_Content()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetContent");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Ambiguous, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
        }

        [Fact]
        public async Task ApiController_CreatedRelative()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedRelative");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
            Assert.Equal("5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedAbsolute()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedAbsolute");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
            Assert.Equal("/api/Blog/ActionResult/GetUser/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedQualified()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedQualified");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
            Assert.Equal("http://localhost/api/Blog/ActionResult/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedUri()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedUri");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
            Assert.Equal("/api/Blog/ActionResult/GetUser/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_CreatedAtRoute()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetCreatedAtRoute");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
            Assert.Equal("http://localhost/api/Blog/ActionResult/GetUser/5", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_InternalServerError()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetInternalServerError");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_InternalServerErrorException()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetInternalServerErrorException");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("{\"Message\":\"An error has occurred.\"}", content);
        }

        [Fact]
        public async Task ApiController_Json()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetJson");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
        }

        [Fact]
        public async Task ApiController_JsonSettings()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected =
                "{" + Environment.NewLine +
                "  \"Name\": \"Test User\"" + Environment.NewLine +
                "}";

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetJsonSettings");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_JsonSettingsEncoding()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected =
                "{" + Environment.NewLine +
                "  \"Name\": \"Test User\"" + Environment.NewLine +
                "}";

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetJsonSettingsEncoding");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
            Assert.Equal("utf-32", response.Content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task ApiController_NotFound()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetNotFound");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_Ok()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetOk");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_OkContent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetOkContent");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("{\"Name\":\"Test User\"}", content);
        }

        [Fact]
        public async Task ApiController_RedirectString()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetRedirectString");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("http://localhost/api/Users", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_RedirectUri()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetRedirectUri");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("api/Blog", response.Headers.Location.OriginalString);
        }

        [Fact]
        public async Task ApiController_ResponseMessage()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetResponseMessage");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new string[] { "Hello" }, response.Headers.GetValues("X-Test"));
        }

        [Fact]
        public async Task ApiController_StatusCode()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/ActionResult/GetStatusCode");

            // Assert
            Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        }
    }
}