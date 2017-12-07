// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}