// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.Routing.FunctionalTests
{
    public class DispatcherSampleTest : IDisposable
    {
        private readonly HttpClient _client;
        private readonly TestServer _testServer;

        public DispatcherSampleTest()
        {
            var webHostBuilder = DispatcherSample.Web.Program.GetWebHostBuilder();
            _testServer = new TestServer(webHostBuilder);
            _client = _testServer.CreateClient();
            _client.BaseAddress = new Uri("http://localhost");
        }

        [Fact]
        public async Task MatchesRootPath_AndReturnsPlaintext()
        {
            // Arrange
            var expectedContentType = "text/plain";
            var expectedContent = "Dispatcher sample endpoints:" + Environment.NewLine + "/plaintext";

            // Act
            var response = await _client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType.MediaType);
            var actualContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, actualContent);
        }

        [Fact]
        public async Task MatchesStaticRouteTemplate_AndReturnsPlaintext()
        {
            // Arrange
            var expectedContentType = "text/plain";
            var expectedContent = "Hello, World!";

            // Act
            var response = await _client.GetAsync("/plaintext");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType.MediaType);
            var actualContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, actualContent);
        }

        public void Dispose()
        {
            _testServer.Dispose();
            _client.Dispose();
        }
    }
}
