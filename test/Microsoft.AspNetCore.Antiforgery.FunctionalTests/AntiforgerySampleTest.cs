// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery.FunctionalTests
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
            var setCookieHeaderValue = RetrieveAntiforgeryCookie(response);
            Assert.NotNull(setCookieHeaderValue);
            Assert.False(string.IsNullOrEmpty(setCookieHeaderValue.Value));
            Assert.Null(setCookieHeaderValue.Domain);
            Assert.Equal("/", setCookieHeaderValue.Path);
            Assert.False(setCookieHeaderValue.Secure);

            setCookieHeaderValue = RetrieveAntiforgeryToken(response);
            Assert.NotNull(setCookieHeaderValue);
            Assert.False(string.IsNullOrEmpty(setCookieHeaderValue.Value));
            Assert.Null(setCookieHeaderValue.Domain);
            Assert.Equal("/", setCookieHeaderValue.Path);
            Assert.False(setCookieHeaderValue.Secure);
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
            Assert.Contains($"The required antiforgery cookie \"{cookie.Name}\" is not present.", exception.Message);
        }

        [Fact]
        public async Task PostItem_XSRFWorks()
        {
            // Arrange
            var httpResponse = await Client.GetAsync("/Index.html");

            var cookie = RetrieveAntiforgeryCookie(httpResponse);
            var token = RetrieveAntiforgeryToken(httpResponse);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/items");

            httpRequestMessage.Headers.Add("Cookie", $"{cookie.Name}={cookie.Value}");
            httpRequestMessage.Headers.Add("X-XSRF-TOKEN", token.Value);

            // Act
            var response = await Client.SendAsync(httpRequestMessage);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private static SetCookieHeaderValue RetrieveAntiforgeryToken(HttpResponseMessage response)
        {
            return response.Headers.GetValues(HeaderNames.SetCookie)
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
                .Where(setCookieHeaderValue => setCookieHeaderValue.Name == "XSRF-TOKEN")
                .FirstOrDefault();
        }

        private static SetCookieHeaderValue RetrieveAntiforgeryCookie(HttpResponseMessage response)
        {
            return response.Headers.GetValues(HeaderNames.SetCookie)
                .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
                .Where(setCookieHeaderValue => setCookieHeaderValue.Name.StartsWith(".AspNetCore.Antiforgery."))
                .FirstOrDefault();
        }
    }
}