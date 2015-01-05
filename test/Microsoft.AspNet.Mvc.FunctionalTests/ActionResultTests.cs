// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ActionResultsWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ActionResultTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ActionResultsWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task BadRequestResult_CanBeReturned()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
            var server = TestServer.Create(_provider, _app);
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
    }
}