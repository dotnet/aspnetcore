// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class GlobalAuthorizationFilterTest : IClassFixture<MvcTestFixture<SecurityWebSite.StartupWithGlobalDenyAnonymousFilter>>
    {
        public GlobalAuthorizationFilterTest(MvcTestFixture<SecurityWebSite.StartupWithGlobalDenyAnonymousFilter> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task DeniesAnonymousUsers_ByDefault()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Administration/Index");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal(
                "http://localhost/Home/Login?ReturnUrl=%2FAdministration%2FIndex",
                response.Headers.Location.ToString());
        }

        [Fact]
        public async Task AllowAnonymousUsers_ForActionsWithAllowAnonymousAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Administration/AllowAnonymousAction");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Administration.AllowAnonymousAction", body);
        }

        [Fact]
        public async Task AuthorizationPoliciesDoNotCombineByDefault()
        {
            // Arrange & Act
            var response = await Client.PostAsync("http://localhost/Administration/SignInCookie2", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"));

            var cookie2 = response.Headers.GetValues("Set-Cookie").SingleOrDefault();

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Administration/EitherCookie");
            request.Headers.Add("Cookie", cookie2);

            // Will fail because default cookie is not sent so [Authorize] fails
            response = await Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal(
                "http://localhost/Home/Login?ReturnUrl=%2FAdministration%2FEitherCookie",
                response.Headers.Location.ToString());
         }
        
    }
}