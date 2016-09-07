// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataInCookiesTest : TempDataTestBase, IClassFixture<MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProvider>>
    {
        private const int DefaultChunkSize = 4070;

        public TempDataInCookiesTest(MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProvider> fixture)
        {
            Client = fixture.Client;
        }

        protected override HttpClient Client { get; }

        [Theory]
        [InlineData(DefaultChunkSize)]
        [InlineData(DefaultChunkSize * 1.5)]
        [InlineData(DefaultChunkSize * 2)]
        [InlineData(DefaultChunkSize * 3)]
        public async Task RoundTripLargeData_WorksWithChunkingCookies(int size)
        {
            // Arrange
            var character = 'a';
            var expected = new string(character, size);

            // Act 1
            var response = await Client.GetAsync($"/TempData/SetLargeValueInTempData?size={size}&character={character}");

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

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