// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataInCookiesTest : TempDataTestBase, IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public TempDataInCookiesTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        protected override HttpClient Client { get; }


        [Theory]
        [InlineData(ChunkingCookieManager.DefaultChunkSize)]
        [InlineData(ChunkingCookieManager.DefaultChunkSize * 1.5)]
        [InlineData(ChunkingCookieManager.DefaultChunkSize * 2)]
        [InlineData(ChunkingCookieManager.DefaultChunkSize * 3)]
        public async Task RoundTripLargeData_WorksWithChunkingCookies(int size)
        {
            // Arrange
            var character = 'a';
            var expected = new string(character, size);

            // Act 1
            var response = await Client.GetAsync($"/TempData/SetLargeValueInTempData?size={size}&character={character}");

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieValues));
            setCookieValues = setCookieValues.Where(cookie => cookie.Contains(CookieTempDataProvider.CookieName));
            Assert.NotEmpty(setCookieValues);
            // Verify that all the cookies from CookieTempDataProvider are within the maximum size
            foreach (var cookie in setCookieValues)
            {
                Assert.True(cookie.Length <= ChunkingCookieManager.DefaultChunkSize);
            }

            var cookieTempDataProviderCookies = setCookieValues
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue));
            foreach (var cookieTempDataProviderCookie in cookieTempDataProviderCookies)
            {
                Assert.NotNull(cookieTempDataProviderCookie.Value.Value);
                Assert.Equal("/", cookieTempDataProviderCookie.Path);
                Assert.Null(cookieTempDataProviderCookie.Domain.Value);
                Assert.False(cookieTempDataProviderCookie.Secure);
            }

            // Act 2
            response = await Client.SendAsync(GetRequest("/TempData/GetLargeValueFromTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, body);
            Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out setCookieValues));
            var setCookieHeaderValue = setCookieValues
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
                .FirstOrDefault(setCookieHeader => setCookieHeader.Name == CookieTempDataProvider.CookieName);
            Assert.NotNull(setCookieHeaderValue);
            Assert.Equal(string.Empty, setCookieHeaderValue.Value);
            Assert.Equal("/", setCookieHeaderValue.Path);
            Assert.Null(setCookieHeaderValue.Domain.Value);
            Assert.NotNull(setCookieHeaderValue.Expires);
            Assert.True(setCookieHeaderValue.Expires < DateTimeOffset.Now); // expired cookie

            // Act 3
            response = await Client.SendAsync(GetRequest("/TempData/GetLargeValueFromTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Redirect_RetainsTempData_EvenIfAccessed_AndSetsAppropriateCookieValues()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await Client.PostAsync("/TempData/SetTempData", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieValues));
            var setCookieHeader = setCookieValues
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
                .FirstOrDefault(setCookieHeaderValue => setCookieHeaderValue.Name == CookieTempDataProvider.CookieName);
            Assert.NotNull(setCookieHeader);
            Assert.Equal("/", setCookieHeader.Path);
            Assert.Null(setCookieHeader.Domain.Value);
            Assert.False(setCookieHeader.Secure);
            Assert.Null(setCookieHeader.Expires);

            // Act 2
            var redirectResponse = await Client.SendAsync(GetRequest("/TempData/GetTempDataAndRedirect", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

            // Act 3
            response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), response));

            // Assert 3
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
            Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out setCookieValues));
            setCookieHeader = setCookieValues
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
                .FirstOrDefault(setCookieHeaderValue => setCookieHeaderValue.Name == CookieTempDataProvider.CookieName);
            Assert.NotNull(setCookieHeader);
            Assert.Equal(string.Empty, setCookieHeader.Value);
            Assert.Equal("/", setCookieHeader.Path);
            Assert.Null(setCookieHeader.Domain.Value);
            Assert.NotNull(setCookieHeader.Expires);
            Assert.True(setCookieHeader.Expires < DateTimeOffset.Now); // expired cookie
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CookieTempDataProviderCookie_DoesNotSetsSecureAttributeOnCookie(bool secureRequest)
        {
            // Arrange
            var protocol = secureRequest ? "https" : "http";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await Client.PostAsync($"{protocol}://localhost/TempData/SetTempData", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieValues));
            var setCookieHeader = setCookieValues
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
                .FirstOrDefault(setCookieHeaderValue => setCookieHeaderValue.Name == CookieTempDataProvider.CookieName);
            Assert.NotNull(setCookieHeader);
            Assert.Equal("/", setCookieHeader.Path);
            Assert.Null(setCookieHeader.Domain.Value);
            Assert.False(setCookieHeader.Secure);
            Assert.Null(setCookieHeader.Expires);
        }
    }
}