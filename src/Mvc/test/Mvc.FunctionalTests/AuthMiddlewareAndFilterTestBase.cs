// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public abstract class AuthMiddlewareAndFilterTestBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
    {
        protected AuthMiddlewareAndFilterTestBase(MvcTestFixture<TStartup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<TStartup>();

        public HttpClient Client { get; }

        [Fact]
        public async Task AllowAnonymousOnActionsWork()
        {
            // Arrange & Act
            var response = await Client.GetAsync("AuthorizedActions/ActionWithoutAllowAnonymous");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GlobalAuthFilter_AppliesToActionsWithoutAnyAuthAttributes()
        {
            var action = "AuthorizedActions/ActionWithoutAuthAttribute";
            var response = await Client.GetAsync(action);

            await AssertAuthorizeResponse(response);

            // We should be able to login with ClaimA alone
            var authCookie = await GetAuthCookieAsync("LoginClaimA");

            var request = new HttpRequestMessage(HttpMethod.Get, action);
            request.Headers.Add("Cookie", authCookie);

            response = await Client.SendAsync(request);
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GlobalAuthFilter_CombinesWithAuthAttributeSpecifiedOnAction()
        {
            var action = "AuthorizedActions/ActionWithAuthAttribute";
            var response = await Client.GetAsync(action);

            await AssertAuthorizeResponse(response);

            // LoginClaimA should be enough for the global auth filter, but not for the auth attribute on the action.
            var authCookie = await GetAuthCookieAsync("LoginClaimA");

            var request = new HttpRequestMessage(HttpMethod.Get, action);
            request.Headers.Add("Cookie", authCookie);

            response = await Client.SendAsync(request);
            await AssertAuthorizeResponse(response);

            authCookie = await GetAuthCookieAsync("LoginClaimAB");
            request = new HttpRequestMessage(HttpMethod.Get, action);
            request.Headers.Add("Cookie", authCookie);

            response = await Client.SendAsync(request);
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AllowAnonymousOnPageConfiguredViaConventionWorks()
        {
            // Arrange & Act
            var response = await Client.GetAsync("AllowAnonymousPageViaConvention");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AllowAnonymousOnPageConfiguredViaModelWorks()
        {
            // Arrange & Act
            var response = await Client.GetAsync("AllowAnonymousPageViaModel");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GlobalAuthFilterAppliedToPageWorks()
        {
            // Arrange & Act
            var response = await Client.GetAsync("PagesHome");

            // Assert
            await AssertAuthorizeResponse(response);

            // We should be able to login with ClaimA alone
            var authCookie = await GetAuthCookieAsync("LoginClaimA");

            var request = new HttpRequestMessage(HttpMethod.Get, "PagesHome");
            request.Headers.Add("Cookie", authCookie);

            response = await Client.SendAsync(request);
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        [Fact]
        public Task GlobalAuthFilter_CombinesWithAuthAttributeOnPageModel()
        {
            // Arrange
            var page = "AuthorizePageViaModel";

            return LoginAB(page);
        }

        [Fact]
        public Task GlobalAuthFilter_CombinesWithAuthAttributeSpecifiedViaConvention()
        {
            // Arrange
            var page = "AuthorizePageViaConvention";

            return LoginAB(page);
        }

        private async Task LoginAB(string url)
        {
            var response = await Client.GetAsync(url);

            // Assert
            await AssertAuthorizeResponse(response);

            // ClaimA should be insufficient
            var authCookie = await GetAuthCookieAsync("LoginClaimA");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", authCookie);

            response = await Client.SendAsync(request);
            await AssertAuthorizeResponse(response);

            authCookie = await GetAuthCookieAsync("LoginClaimAB");
            request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", authCookie);

            response = await Client.SendAsync(request);
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        }

        private async Task AssertAuthorizeResponse(HttpResponseMessage response)
        {
            await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
            Assert.Equal("/Account/Login", response.Headers.Location.LocalPath);
        }

        private async Task<string> GetAuthCookieAsync(string action)
        {
            var response = await Client.PostAsync($"Login/{action}", null);

            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.True(response.Headers.Contains("Set-Cookie"));
            return response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        }
    }
}
