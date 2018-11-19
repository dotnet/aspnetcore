// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            Client = fixture.CreateClient();
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("http://localhost/account/signin?ReturnUrl=%2FHome%2FMinimumAge10", response.RequestMessage.RequestUri.ToString());
        }

        [Fact]
        public async Task MinimumAge10WorksIfOldEnough()
        {
            // Arrange & Act
            var signIn = await SignIn(Client, "Dude", DateTime.Now.Subtract(TimeSpan.FromDays(365 * 20)).ToShortDateString());
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

            var response = await Client.GetAsync("/Home/MinimumAge10");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Welcome, Dude", content);
            Assert.Contains("Welcome to a page restricted to users 10 or older", content);
        }

        [Fact]
        public async Task MinimumAge10FailsIfNotOldEnough()
        {
            // Arrange & Act
            var signIn = await SignIn(Client, "Dude", DateTime.Now.Subtract(TimeSpan.FromDays(365 * 5)).ToShortDateString());
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

            var response = await Client.GetAsync("/Home/MinimumAge10");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Access Denied: Dude is not authorized to view this page.", content);
        }

        [Fact]
        public async Task MinimumAge50WorksIfOldEnough()
        {
            // Arrange & Act
            var signIn = await SignIn(Client, "Dude", DateTime.Now.Subtract(TimeSpan.FromDays(365 * 55)).ToShortDateString());
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

            var response = await Client.GetAsync("/Home/MinimumAge50");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Welcome, Dude", content);
            Assert.Contains("Welcome to a page restricted to users 50 or older", content);
        }

        [Fact]
        public async Task MinimumAge50FailsIfNotOldEnough()
        {
            // Arrange & Act
            var signIn = await SignIn(Client, "Dude", DateTime.Now.Subtract(TimeSpan.FromDays(365 * 20)).ToShortDateString());
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);

            var response = await Client.GetAsync("/Home/MinimumAge50");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Access Denied: Dude is not authorized to view this page.", content);
        }

        [Fact]
        public async Task MinimumAge50RedirectsWhenNotLoggedIn()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/Home/MinimumAge50");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("http://localhost/account/signin?ReturnUrl=%2FHome%2FMinimumAge50", response.RequestMessage.RequestUri.ToString());
        }

        internal static async Task<HttpResponseMessage> SignIn(HttpClient client, string userName, string dob)
        {
            var goToSignIn = await client.GetAsync("/account/signin");
            var signIn = await TestAssert.IsHtmlDocumentAsync(goToSignIn);

            var form = TestAssert.HasForm(signIn);
            return await client.SendAsync(form, new Dictionary<string, string>()
            {
                ["UserName"] = userName,
                ["DOB"] = dob,
            });
        }
    }
}
