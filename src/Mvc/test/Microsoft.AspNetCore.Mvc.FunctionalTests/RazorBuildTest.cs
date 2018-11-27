// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RazorBuildTest : IClassFixture<MvcTestFixture<RazorBuildWebSite.Startup>>
    {
        public RazorBuildTest(MvcTestFixture<RazorBuildWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task PrecompiledPage_LocalPageWithDifferentContent_NotUsed()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/Precompilation/Page");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from buildtime-compiled precompilation page!", responseBody.Trim());
        }

        [Fact]
        public async Task PrecompiledView_LocalViewWithDifferentContent_NotUsed()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/Precompilation/View");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from buildtime-compiled precompilation view!", responseBody.Trim());
        }

        [Fact(Skip = "https://github.com/aspnet/Mvc/issues/8753")]
        public async Task Rzc_LocalPageWithDifferentContent_IsUsed()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/Rzc/Page");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from runtime-compiled rzc page!", responseBody.Trim());
        }

        [Fact(Skip = "https://github.com/aspnet/Mvc/issues/8753")]
        public async Task Rzc_LocalViewWithDifferentContent_IsUsed()
        {
            // Act
            var response = await Client.GetAsync("http://localhost/Rzc/View");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from runtime-compiled rzc view!", responseBody.Trim());
        }

        [Fact]
        public async Task RzcViewsArePreferredToPrecompiledViews()
        {
            // Verifies that when two views have the same paths, the one compiled using rzc is preferred to the one from Precompilation.
            // Act
            var response = await Client.GetAsync("http://localhost/Common/View");
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from buildtime-compiled rzc view!", responseBody.Trim());
        }
    }
}
