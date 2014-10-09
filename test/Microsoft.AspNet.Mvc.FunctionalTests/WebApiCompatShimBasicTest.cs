// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class WebApiCompatShimBasicTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(nameof(WebApiCompatShimWebSite));
        private readonly Action<IApplicationBuilder> _app = new WebApiCompatShimWebSite.Startup().Configure;

        [Fact]
        public async Task ApiController_Activates_HttpContextAndUser()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/BasicApi/WriteToHttpContext");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Hello, Anonymous User from WebApiCompatShimWebSite.BasicApiController.WriteToHttpContext", 
                content);
        }

        [Fact]
        public async Task ApiController_Activates_UrlHelper()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/BasicApi/GenerateUrl");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Visited: /api/Blog/BasicApi/GenerateUrl",
                content);
        }

        [Fact]
        public async Task Options_SetsDefaultFormatters()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected = new string[]
            {
                typeof(JsonMediaTypeFormatter).FullName,
                typeof(XmlMediaTypeFormatter).FullName,
                typeof(FormUrlEncodedMediaTypeFormatter).FullName,
            };

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/BasicApi/GetFormatters");
            var content = await response.Content.ReadAsStringAsync();

            var formatters = JsonConvert.DeserializeObject<string[]>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, formatters);
        }

        [Fact]
        public async Task ApiController_CanValidateCustomObjectWithPrefix_Fails()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync(
                "http://localhost/api/Blog/BasicApi/ValidateObjectWithPrefixFails?prefix=prefix");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(1, json.Count);
            Assert.Equal("The field ID must be between 0 and 100.", json["prefix.ID"]);
        }
        
        [Fact]
        public async Task ApiController_CanValidateCustomObject_IsSuccessFul()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/api/Blog/BasicApi/ValidateObject_Passes");

            // Assert
            Assert.Equal("true", response);
        }

        [Fact]
        public async Task ApiController_CanValidateCustomObject_Fails()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/api/Blog/BasicApi/ValidateObjectFails");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Equal(1, json.Count);
            Assert.Equal("The field ID must be between 0 and 100.", json["ID"]);
        }

        [Fact]
        public async Task ApiController_RequestProperty()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected =
                "POST http://localhost/api/Blog/HttpRequestMessage/EchoProperty localhost " +
                "13 Hello, world!";

            // Act
            var response = await client.PostAsync(
                "http://localhost/api/Blog/HttpRequestMessage/EchoProperty",
                new StringContent("Hello, world!"));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_RequestParameter()
        { 
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected =
                "POST http://localhost/api/Blog/HttpRequestMessage/EchoParameter localhost " +
                "17 Hello, the world!";

            // Act
            var response = await client.PostAsync(
                "http://localhost/api/Blog/HttpRequestMessage/EchoParameter",
                new StringContent("Hello, the world!"));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ApiController_ResponseReturned()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected =
                "POST Hello, HttpResponseMessage world!";

            // Act
            var response = await client.PostAsync(
                "http://localhost/api/Blog/HttpRequestMessage/EchoWithResponseMessage",
                new StringContent("Hello, HttpResponseMessage world!"));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);

            IEnumerable<string> values;
            Assert.True(response.Headers.TryGetValues("X-Test", out values));
            Assert.Equal(new string[] { "Hello!" }, values);
            Assert.Equal(38, response.Content.Headers.ContentLength);
        }

        [Fact]
        public async Task ApiController_ResponseReturned_Chunked()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expected =
                "POST Hello, HttpResponseMessage world!";

            // Act
            var response = await client.PostAsync(
                "http://localhost/api/Blog/HttpRequestMessage/EchoWithResponseMessageChunked",
                new StringContent("Hello, HttpResponseMessage world!"));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);

            IEnumerable<string> values;
            Assert.True(response.Headers.TryGetValues("X-Test", out values));
            Assert.Equal(new string[] { "Hello!" }, values);
            Assert.Equal(true, response.Headers.TransferEncodingChunked);
        }
    }
}
#endif