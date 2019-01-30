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
    public class RouterBenchmarkTest : IDisposable
    {
        private readonly HttpClient _client;
        private readonly TestServer _testServer;

        public RouterBenchmarkTest()
        {
            // This switch and value are set by benchmark server when running the app for profiling.
            var args = new[] { "--scenarios", "PlaintextRouting" };
            var webHostBuilder = Benchmarks.Program.GetWebHostBuilder(args);

            // Make sure we are using the right startup
            var startupName = webHostBuilder.GetSetting("Startup");
            Assert.Equal(nameof(Benchmarks.StartupUsingRouter), startupName);

            _testServer = new TestServer(webHostBuilder);
            _client = _testServer.CreateClient();
            _client.BaseAddress = new Uri("http://localhost");
        }

        [Fact]
        public async Task RouteHandlerWritesResponse()
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