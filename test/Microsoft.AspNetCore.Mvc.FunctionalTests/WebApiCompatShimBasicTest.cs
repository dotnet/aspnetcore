// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class WebApiCompatShimBasicTest : IClassFixture<MvcTestFixture<WebApiCompatShimWebSite.Startup>>
    {
        public WebApiCompatShimBasicTest(MvcTestFixture<WebApiCompatShimWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ApiController_Activates_HttpContextAndUser()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/BasicApi/WriteToHttpContext");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Hello, Anonymous User from WebApiCompatShimWebSite.BasicApiController.WriteToHttpContext (WebApiCompatShimWebSite)",
                content);
        }

        [Fact]
        public async Task ApiController_Activates_UrlHelper()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/BasicApi/GenerateUrl");
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
            var expected = new string[]
            {
                typeof(JsonMediaTypeFormatter).FullName,
                typeof(XmlMediaTypeFormatter).FullName,

#if NET461
                // We call into WebAPI and ask it to add all of its formatters. On net461 it adds this additional formatter.
                typeof(FormUrlEncodedMediaTypeFormatter).FullName
#endif
            };

            // Act
            var response = await Client.GetAsync("http://localhost/api/Blog/BasicApi/GetFormatters");
            var content = await response.Content.ReadAsStringAsync();

            var formatters = JsonConvert.DeserializeObject<string[]>(content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, formatters);
        }

        [Fact]
        public async Task ActionThrowsHttpResponseException_WithStatusCode()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/api/Blog/HttpResponseException/ThrowsHttpResponseExceptionWithHttpStatusCode");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public async Task ActionThrowsHttpResponseException_WithResponse()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/api/Blog/HttpResponseException" +
                "/ThrowsHttpResponseExceptionWithHttpResponseMessage?message=send some message");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("send some message", content);
        }

        [Fact]
        public async Task ActionThrowsHttpResponseException_EnsureGlobalHttpresponseExceptionActionFilter_IsInvoked()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
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
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/api/Blog/" +
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
            // Arrange & Act
            var response = await Client.GetStringAsync(
                "http://localhost/api/Blog/BasicApi/ValidateObjectWithPrefixFails?prefix=prefix");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Single(json);
            Assert.Equal("The field ID must be between 0 and 100.", json["prefix.ID"]);
        }

        [Fact]
        public async Task ApiController_CanValidateCustomObject_IsSuccessFul()
        {
            // Arrange & Act
            var response = await Client.GetStringAsync("http://localhost/api/Blog/BasicApi/ValidateObject_Passes");

            // Assert
            Assert.Equal("true", response);
        }

        [Fact]
        public async Task ApiController_CanValidateCustomObject_Fails()
        {
            // Arrange & Act
            var response = await Client.GetStringAsync("http://localhost/api/Blog/BasicApi/ValidateObjectFails");

            // Assert
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            Assert.Single(json);
            Assert.Equal("The field ID must be between 0 and 100.", json["ID"]);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/24
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task ApiController_RequestProperty()
        {
            // Arrange
            var expected = "POST http://localhost/api/Blog/HttpRequestMessage/EchoProperty localhost " +
                "13 Hello, world!";

            // Act
            var response = await Client.PostAsync(
                "http://localhost/api/Blog/HttpRequestMessage/EchoProperty",
                new StringContent("Hello, world!"));
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, content);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/24
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task ApiController_RequestParameter()
        {
            // Arrange
            var expected =
                "POST http://localhost/api/Blog/HttpRequestMessage/EchoParameter localhost " +
                "17 Hello, the world!";

            // Act
            var response = await Client.PostAsync(
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
            var expected = "POST Hello, HttpResponseMessage world!";

            // Act
            var response = await Client.PostAsync(
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
        public async Task ApiController_ExplicitChunkedEncoding_IsIgnored()
        {
            // Arrange
            var expected = "POST Hello, HttpResponseMessage world!";
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("http://localhost/api/Blog/HttpRequestMessage/EchoWithResponseMessageChunked");
            request.Content = new StringContent("Hello, HttpResponseMessage world!");

            // Act
            // HttpClient buffers the response by default and this would set the Content-Length header and so
            // this will not provide us accurate information as to whether the server set the header or
            // the client. So here we explicitly mention to only read the headers and not the body.
            var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            if (!TestPlatformHelper.IsMono)
            {
                // Mono issue - https://github.com/aspnet/External/issues/20
                Assert.NotNull(response.Content.Headers.ContentLength);
            }

            Assert.Null(response.Headers.TransferEncodingChunked);

            // When HttpClient by default reads and buffers the response body, it disposes the
            // response stream for us. But since we are reading the content explicitly, we need
            // to close it.
            var responseStream = await response.Content.ReadAsStreamAsync();
            using (var streamReader = new StreamReader(responseStream))
            {
                Assert.Equal(expected, streamReader.ReadToEnd());
            }

            IEnumerable<string> values;
            Assert.True(response.Headers.TryGetValues("X-Test", out values));
            Assert.Equal(new string[] { "Hello!" }, values);
        }

        [Theory]
        [InlineData("application/json", "application/json")]
        [InlineData("text/xml", "text/xml")]
        [InlineData("text/plain, text/xml; q=0.5", "text/xml")]
        [InlineData("application/*", "application/json")]
        public async Task ApiController_CreateResponse_Conneg(string accept, string mediaType)
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/GetUser");
            request.Headers.Accept.ParseAdd(accept);

            // Act
            var response = await Client.SendAsync(request);
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
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/GetUser?mediaType=" + mediaType);

            // Act
            var response = await Client.SendAsync(request);
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
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/Fail");
            request.Headers.Accept.ParseAdd(accept);

            // Act
            var response = await Client.SendAsync(request);
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
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/api/Blog/HttpRequestMessage/GetUserJson");

            // Accept header will be ignored
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            // Act
            var response = await Client.SendAsync(request);
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
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(expected, response.StatusCode);
        }

        [Theory]
        [InlineData("http://localhost/BasicApi/GenerateUrl", HttpStatusCode.NotFound)]
        [InlineData("http://localhost/api/Blog/BasicApi/GenerateUrl", HttpStatusCode.OK)]
        public async Task WebApiRouting_AccessWebApiController(string url, HttpStatusCode expected)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task ApiController_Returns_ByteArrayContent()
        {
            // Arrange
            var expectedBody = "Hello from ByteArrayContent!!";

            // Act
            var response = await Client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/ReturnByteArrayContent");

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
            var expectedBody = "This content is from a file";

            // Act
            var response = await Client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/ReturnStreamContent");

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
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/api/Blog/HttpRequestMessage/" + action);

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
            var expectedBody = "Hello from PushStreamContent with custom headers!!";
            var multipleValues = new[] { "value1", "value2" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/api/Blog/HttpRequestMessage/ReturnPushStreamContentWithCustomHeaders");

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
