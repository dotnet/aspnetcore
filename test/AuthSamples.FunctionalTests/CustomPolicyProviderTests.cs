// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests
{
    public class CustomPolicyProviderTests : IClassFixture<WebApplicationFactory<CustomPolicyProvider.Startup>>
    {
        public CustomPolicyProviderTests(WebApplicationFactory<CustomPolicyProvider.Startup> fixture)
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
        public async Task MinimumAge10RedirectsWhenNotLoggedIn()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/Home/MinimumAge10");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("http://localhost/account/signin?ReturnUrl=%2FHome%2FMinimumAge10", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task MinimumAge50RedirectsWhenNotLoggedIn()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/Home/MinimumAge50");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("http://localhost/account/signin?ReturnUrl=%2FHome%2FMinimumAge50", response.Headers.Location.ToString());
        }
    }
}
