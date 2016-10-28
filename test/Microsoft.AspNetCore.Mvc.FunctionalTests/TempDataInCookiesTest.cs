// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataInCookiesTest : TempDataTestBase, IClassFixture<MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProvider>>
    {
        public TempDataInCookiesTest(MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProvider> fixture)
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
            var cookies = response.Headers.GetValues(HeaderNames.SetCookie);
            var cookieTempDataProviderCookies = cookies.Where(cookie => cookie.Contains(CookieTempDataProvider.CookieName));
            Assert.NotNull(cookieTempDataProviderCookies);

            // Verify that all the cookies from CookieTempDataProvider are within the maximum size
            foreach (var cookie in cookieTempDataProviderCookies)
            {
                Assert.True(cookie.Length <= ChunkingCookieManager.DefaultChunkSize);
            }

            // Act 2
            response = await Client.SendAsync(GetRequest("/TempData/GetLargeValueFromTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, body);

            // Act 3
            response = await Client.SendAsync(GetRequest("/TempData/GetLargeValueFromTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}