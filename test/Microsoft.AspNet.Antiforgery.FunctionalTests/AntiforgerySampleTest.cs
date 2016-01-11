// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Antiforgery.FunctionalTests
{
    public class AntiforgerySampleTests : IClassFixture<AntiForgerySampleTestFixture>
    {
        public AntiforgerySampleTests(AntiForgerySampleTestFixture fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ItemsPage_SetsXSRFTokens()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Index.html");

            // Assert
            var cookie = RetrieveAntiforgeryCookie(response);
            Assert.NotNull(cookie.Value);

            var token = RetrieveAntiforgeryToken(response);
            Assert.NotNull(token.Value);
        }

        [Fact]
        public async Task PostItem_NeedsHeader()
        {
            // Arrange
            var httpResponse = await Client.GetAsync("http://localhost");
            var cookie = RetrieveAntiforgeryCookie(httpResponse);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/items");

            // Act
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(async () =>
            {
                var response = await Client.SendAsync(httpRequestMessage);
            });

            // Assert
            Assert.Contains($"The required antiforgery cookie \"{cookie.Key}\" is not present.", exception.Message);
        }

        [Fact]
        public async Task PostItem_XSRFWorks()
        {
            // Arrange
            var httpResponse = await Client.GetAsync("/Index.html");

            var cookie = RetrieveAntiforgeryCookie(httpResponse);
            var token = RetrieveAntiforgeryToken(httpResponse);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/items");

            httpRequestMessage.Headers.Add("X-XSRF-TOKEN", token.Value);
            httpRequestMessage.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private static KeyValuePair<string, string> RetrieveAntiforgeryToken(HttpResponseMessage response)
        {
            return GetCookie(response, 1);
        }

        private static KeyValuePair<string, string> RetrieveAntiforgeryCookie(HttpResponseMessage response)
        {
            return GetCookie(response, 0);
        }

        private static KeyValuePair<string, string> GetCookie(HttpResponseMessage response, int index)
        {
            var setCookieArray = response.Headers.GetValues("Set-Cookie").ToArray();
            var cookie = setCookieArray[index].Split(';').First().Split('=');
            var cookieKey = cookie[0];
            var cookieData = cookie[1];

            return new KeyValuePair<string, string>(cookieKey, cookieData);
        }
    }
}