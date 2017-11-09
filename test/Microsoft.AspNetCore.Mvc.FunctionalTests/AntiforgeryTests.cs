// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class AntiforgeryTests : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public AntiforgeryTests(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task MultipleAFTokensWithinTheSamePage_GeneratesASingleCookieToken()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Antiforgery/Login");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
            Assert.Equal("SAMEORIGIN", header);

            var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();

            // Even though there are two forms there should only be one response cookie,
            // as for the second form, the cookie from the first token should be reused.
            Assert.Single(setCookieHeader);

            Assert.True(response.Headers.CacheControl.NoCache);
            var pragmaValue = Assert.Single(response.Headers.Pragma.ToArray());
            Assert.Equal("no-cache", pragmaValue.Name);
        }

        [Fact]
        public async Task MultipleFormPostWithingASingleView_AreAllowed()
        {
            // Arrange
            // Do a get request.
            var getResponse = await Client.GetAsync("http://localhost/Antiforgery/Login");
            var responseBody = await getResponse.Content.ReadAsStringAsync();

            // Get the AF token for the second login. If the cookies are generated twice(i.e are different),
            // this AF token will not work with the first cookie.
            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
                responseBody,
                "/Antiforgery/UseFacebookLogin");
            var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/Login");
            request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("OK", await response.Content.ReadAsStringAsync());
        }

        [Fact(Skip = "https://github.com/aspnet/Mvc/issues/7040")]
        public async Task SetCookieAndHeaderBeforeFlushAsync_GeneratesCookieTokenAndHeader()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Antiforgery/FlushAsyncLogin");

            // Assert
            var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
            Assert.Equal("SAMEORIGIN", header);

            var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();
            Assert.Single(setCookieHeader);

            Assert.True(response.Headers.CacheControl.NoCache);
            var pragmaValue = Assert.Single(response.Headers.Pragma.ToArray());
            Assert.Equal("no-cache", pragmaValue.Name);
        }

        [Fact(Skip = "https://github.com/aspnet/Mvc/issues/7040")]
        public async Task SetCookieAndHeaderBeforeFlushAsync_PostToForm()
        {
            // Arrange
            // do a get response.
            var getResponse = await Client.GetAsync("http://localhost/Antiforgery/FlushAsyncLogin");
            var responseBody = await getResponse.Content.ReadAsStringAsync();

            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
                responseBody,
                "Antiforgery/FlushAsyncLogin");
            var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/FlushAsyncLogin");
            request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "test"),
                new KeyValuePair<string,string>("Password", "password"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("OK", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Antiforgery_HeaderNotSet_SendsBadRequest()
        {
            // Arrange
            var getResponse = await Client.GetAsync("http://localhost/Antiforgery/Login");
            var responseBody = await getResponse.Content.ReadAsStringAsync();

            var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
                responseBody,
                "Antiforgery/Login");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/Login");
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "test"),
                new KeyValuePair<string,string>("Password", "password"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AntiforgeryTokenGeneration_SetsDoNotCacheHeaders_OverridesExistingCachingHeaders()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Antiforgery/AntiforgeryTokenAndResponseCaching");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
            Assert.Equal("SAMEORIGIN", header);

            var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();

            // Even though there are two forms there should only be one response cookie,
            // as for the second form, the cookie from the first token should be reused.
            Assert.Single(setCookieHeader);

            Assert.True(response.Headers.CacheControl.NoCache);
            var pragmaValue = Assert.Single(response.Headers.Pragma.ToArray());
            Assert.Equal("no-cache", pragmaValue.Name);
        }
    }
}