// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests
{
    public class PathSchemeSelectionTests : IClassFixture<WebApplicationFactory<PathSchemeSelection.Startup>>
    {
        public PathSchemeSelectionTests(WebApplicationFactory<PathSchemeSelection.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task DefaultReturns200()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApiDefaultReturns200()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/api");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task MyClaimsRedirectsToLoginPageWhenNotLoggedIn()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/Home/MyClaims");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("http://localhost/account/login?ReturnUrl=%2FHome%2FMyClaims", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task ApiMyClaimsReturnsClaim()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/api/Home/MyClaims");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Scheme: Api", content); // expected scheme
            Assert.Contains("Hao", content); // expected name claim
        }

    }
}
