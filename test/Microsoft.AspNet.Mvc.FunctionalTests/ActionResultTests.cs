// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ActionResultsWebSite;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ActionResultTests
    {
        private const string SiteName = nameof(ActionResultsWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task BadRequestResult_CanBeReturned()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var input = "{\"SampleInt\":10}";

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetBadResult");

            request.Content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsRelativePathInLocationHeader()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedRelative");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsAbsolutePathInLocationHeader()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAbsolute");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedResult_SetsQualifiedPathInLocationHeader()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedQualified");

            // Act
            var response = await client.SendAsync(request);

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedUri");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtActionResult_GeneratesUri_WithActionAndController()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAtAction");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtRouteResult_GeneratesUri_WithRouteValues()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAtRoute");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CreatedAtRouteResult_GeneratesUri_WithRouteName()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetCreatedAtRouteWithRouteName");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("http://localhost/foo/ActionResultsVerification/GetDummy/1", response.Headers.Location.OriginalString);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SerializableError_CanSerializeNormalObjects()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/ActionResultsWebSite\">" +
                "<SampleInt>2</SampleInt><SampleString>foo</SampleString></DummyClass>";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Home/GetCustomErrorObject");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;charset=utf-8"));
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("[\"Something went wrong with the model.\"]",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsDefaultContentTypeAndEncoding()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            "http://localhost/ActionResultsVerification/GetContentResult");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsContentTypeWithDefaultEncoding()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            "http://localhost/ActionResultsVerification/GetContentResultWithContentType");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            Assert.Equal("content", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ContentResult_WritesContent_SetsContentTypeAndEncoding()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            "http://localhost/ActionResultsVerification/GetContentResultWithContentTypeAndEncoding");

            // Act
            var response = await client.SendAsync(request);

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                            HttpMethod.Get,
                            "http://localhost/ActionResultsVerification/GetObjectResultWithNoContent");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_NoResponseContent()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost/ActionResultsVerification/GetNotFoundObjectResult");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task HttpNotFoundObjectResult_WithResponseContent()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var input = "{\"SampleInt\":10}";

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ActionResultsVerification/GetNotFoundObjectResultWithContent");
            request.Content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("{\"SampleInt\":10,\"SampleString\":\"Foo\"}", await response.Content.ReadAsStringAsync());
        }
    }
}