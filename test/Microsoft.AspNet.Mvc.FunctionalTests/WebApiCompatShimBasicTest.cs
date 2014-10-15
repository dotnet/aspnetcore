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

#if !ASPNETCORE50

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

#endif

        [Fact]
        public async Task ActionThrowsHttpResponseException_WithStatusCode()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/api/Blog/HttpResponseException/ThrowsHttpResponseExceptionWithHttpStatusCode");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public async Task ActionThrowsHttpResponseException_WithResponse()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/api/Blog/HttpResponseException"+
                "/ThrowsHttpResponseExceptionWithHttpResponseMessage?message=send some message");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("send some message", content);
        }

        [Fact]
        public async Task ActionThrowsHttpResponseException_EnsureGlobalHttpresponseExceptionActionFilter_IsInvoked()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/api/Blog/HttpResponseException/ThrowsHttpResponseExceptionEnsureGlobalFilterRunsLast");

            // Assert
            // Ensure we do not get a no content result.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public async Task ActionThrowsHttpResponseException_EnsureGlobalFilterConvention_IsApplied()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/api/Blog/"+
                "HttpResponseException/ThrowsHttpResponseExceptionInjectAFilterToHandleHttpResponseException");

            // Assert
            // Ensure we do get a no content result.
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
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

        [Theory]
        [InlineData("application/json", "application/json")]
        [InlineData("text/xml", "text/xml")]
        [InlineData("text/plain, text/xml; q=0.5", "text/xml")]
        [InlineData("application/*", "application/json")]
        public async Task ApiController_CreateResponse_Conneg(string accept, string mediaType)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get, 
                "http://localhost/api/Blog/HttpRequestMessage/GetUser");

            request.Headers.Accept.ParseAdd(accept);

            // Act
            var response = await client.SendAsync(request);
            var user = await response.Content.ReadAsAsync<WebApiCompatShimWebSite.User>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Test User", user.Name);
            Assert.Equal(mediaType, response.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("text/xml")]
        public async Task ApiController_CreateResponse_HardcodedMediaType(string mediaType)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/GetUser?mediaType=" + mediaType);

            // Act
            var response = await client.SendAsync(request);
            var user = await response.Content.ReadAsAsync<WebApiCompatShimWebSite.User>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Test User", user.Name);
            Assert.Equal(mediaType, response.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData("application/json", "application/json")]
        [InlineData("text/xml", "text/xml")]
        [InlineData("text/plain, text/xml; q=0.5", "text/xml")]
        [InlineData("application/*", "application/json")]
        public async Task ApiController_CreateResponse_Conneg_Error(string accept, string mediaType)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/Fail");

            request.Headers.Accept.ParseAdd(accept);

            // Act
            var response = await client.SendAsync(request);
            var error = await response.Content.ReadAsAsync<HttpError>();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("It failed.", error.Message);
            Assert.Equal(mediaType, response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task ApiController_CreateResponse_HardcodedFormatter()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/GetUserJson");

            // Accept header will be ignored
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            // Act
            var response = await client.SendAsync(request);
            var user = await response.Content.ReadAsAsync<WebApiCompatShimWebSite.User>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Test User", user.Name);
            Assert.Equal("text/json", response.Content.Headers.ContentType.MediaType);
        }

        [Theory]
        [InlineData("http://localhost/Mvc/Index", HttpStatusCode.OK)]
        [InlineData("http://localhost/api/Blog/Mvc/Index", HttpStatusCode.NotFound)]
        public async Task WebApiRouting_AccessMvcController(string url, HttpStatusCode expected)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(expected, response.StatusCode);
        }

        [Theory]
        [InlineData("http://localhost/BasicApi/GenerateUrl", HttpStatusCode.NotFound)]
        [InlineData("http://localhost/api/Blog/BasicApi/GenerateUrl", HttpStatusCode.OK)]
        public async Task WebApiRouting_AccessWebApiController(string url, HttpStatusCode expected)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_Returns_ByteArrayContent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedBody = "Hello from ByteArrayContent!!";

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/ReturnByteArrayContent");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }

        [Fact]
        public async Task ApiController_Returns_StreamContent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedBody = "This content is from a file";

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/ReturnStreamContent");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType.MediaType);
            Assert.NotNull(response.Content.Headers.ContentDisposition);
            Assert.Equal("attachment", response.Content.Headers.ContentDisposition.DispositionType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }

        [Theory]
        [InlineData("ReturnPushStreamContent", "Hello from PushStreamContent!!")]
        [InlineData("ReturnPushStreamContentSync", "Hello from PushStreamContent Sync!!")]
        public async Task ApiController_Returns_PushStreamContent(string action, string expectedBody)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            
            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/" + action );

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }

        [Fact]
        public async Task ApiController_Returns_PushStreamContentWithCustomHeaders()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedBody = "Hello from PushStreamContent with custom headers!!";
            var multipleValues = new[] { "value1", "value2" };

            // Act
            var response = await client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/ReturnPushStreamContentWithCustomHeaders");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.Equal("application/octet-stream", response.Content.Headers.ContentType.ToString());

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
            Assert.Equal(multipleValues, response.Headers.GetValues("Multiple"));
        }
    }
}
