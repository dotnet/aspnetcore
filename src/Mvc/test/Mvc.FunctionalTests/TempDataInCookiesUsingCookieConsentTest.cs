// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataInCookiesUsingCookieConsentTest
        : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProviderAndCookieConsent>>
    {
        private readonly HttpClient _client;

        public TempDataInCookiesUsingCookieConsentTest(
            MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProviderAndCookieConsent> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            _client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<BasicWebSite.StartupWithCookieTempDataProviderAndCookieConsent>();

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/1803")]
        public async Task CookieTempDataProviderCookie_SetInResponse_OnGrantingConsent()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);
            // This response would have the consent cookie which would be sent on rest of the requests here
            var response = await _client.GetAsync("/TempData/GrantConsent");

            // Act 1
            response = await _client.SendAsync(GetPostRequest("/TempData/SetTempData", content, response));

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            response = await _client.SendAsync(GetRequest("/TempData/GetTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);

            // Act 3
            response = await _client.SendAsync(GetRequest("/TempData/GetTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/1803")]
        public async Task CookieTempDataProviderCookie_NotSetInResponse_OnNoConsent()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await _client.PostAsync("/TempData/SetTempData", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            response = await _client.SendAsync(GetRequest("/TempData/GetTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            SetCookieHeaders(request, response);
            return request;
        }

        private HttpRequestMessage GetPostRequest(string path, HttpContent content, HttpResponseMessage response)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Content = content;
            SetCookieHeaders(request, response);
            return request;
        }

        private void SetCookieHeaders(HttpRequestMessage request, HttpResponseMessage response)
        {
            IEnumerable<string> values;
            if (response.Headers.TryGetValues("Set-Cookie", out values))
            {
                foreach (var cookie in SetCookieHeaderValue.ParseList(values.ToList()))
                {
                    if (cookie.Expires == null || cookie.Expires >= DateTimeOffset.UtcNow)
                    {
                        request.Headers.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                    }
                }
            }
        }
    }
}