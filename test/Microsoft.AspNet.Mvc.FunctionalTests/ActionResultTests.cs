// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ActionResultTests : IClassFixture<MvcTestFixture<ActionResultsWebSite.Startup>>
    {
        public ActionResultTests(MvcTestFixture<ActionResultsWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task BadRequestResult_CanBeReturned()
        {
            // Arrange
            var input = "{\"SampleInt\":10}";

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetBadResult");

            request.Content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsRelativePathInLocationHeader()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedRelative");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsAbsolutePathInLocationHeader()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedAbsolute");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsQualifiedPathInLocationHeader()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedQualified");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(
                "http://localhost/ActionResultsVerification/GetDummy/1",
                response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsUriInLocationHeader()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedUri");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtActionResult_GeneratesUri_WithActionAndController()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedAtAction");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtRouteResult_GeneratesUri_WithRouteValues()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedAtRoute");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtRouteResult_GeneratesUri_WithRouteName()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetCreatedAtRouteWithRouteName");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/foo/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SerializableError_CanSerializeNormalObjects()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/ActionResultsWebSite\">" +
                "<SampleInt>2</SampleInt><SampleString>foo</SampleString></DummyClass>";
            var request = new HttpRequestMessage(HttpMethod.Post, "Home/GetCustomErrorObject");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("[\"Something went wrong with the model.\"]",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsDefaultContentTypeAndEncoding()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetContentResult");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsContentType_UsesDefaultEncoding_AndNoCharset()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetContentResultWithContentType");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsContentTypeAndEncoding()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetContentResultWithContentTypeAndEncoding");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("us-ascii", response.Content.Headers.ContentType.CharSet);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ObjectResult_WithStatusCodeAndNoContent_SetsSameStatusCode()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "ActionResultsVerification/GetObjectResultWithNoContent");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_NoResponseContent()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "ActionResultsVerification/GetNotFoundObjectResult");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_WithResponseContent()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "ActionResultsVerification/GetNotFoundObjectResultWithContent");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_WithDisposableObject()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("guid", Guid.NewGuid().ToString()),
            };

            // Act
            var response1 = await Client.PostAsync(
                "/ActionResultsVerification/GetDisposeCalled",
                new FormUrlEncodedContent(nameValueCollection));

            await Client.PostAsync(
                "/ActionResultsVerification/GetNotFoundObjectResultWithDisposableObject",
                new FormUrlEncodedContent(nameValueCollection));

            var response2 = await Client.PostAsync(
                "/ActionResultsVerification/GetDisposeCalled",
                new FormUrlEncodedContent(nameValueCollection));

            // Assert
            var isDisposed = Convert.ToBoolean(await response1.Content.ReadAsStringAsync());
            Assert.False(isDisposed);

            isDisposed = Convert.ToBoolean(await response2.Content.ReadAsStringAsync());
            Assert.True(isDisposed);
        }
    }
}