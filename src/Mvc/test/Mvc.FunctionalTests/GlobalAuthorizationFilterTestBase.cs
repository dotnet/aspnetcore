// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public abstract class GlobalAuthorizationFilterTestBase : IClassFixture<MvcTestFixture<SecurityWebSite.StartupWithGlobalDenyAnonymousFilter>>
    {
        public HttpClient Client { get; protected set; }

        [Fact]
        public virtual async Task DeniesAnonymousUsers_ByDefault()
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
        public async Task AuthorizationPoliciesCombine()
        {
            // Arrange & Act 1
            var response = await Client.PostAsync("http://localhost/Administration/SignInCookie2", null);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"));

            // Arrange 2
            var cookie2 = response.Headers.GetValues("Set-Cookie").SingleOrDefault();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Administration/EitherCookie");
            request.Headers.Add("Cookie", cookie2);

            // Act 2: Will succeed because [Authorize] allows either cookie.
            response = await Client.SendAsync(request);

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

   }
}
