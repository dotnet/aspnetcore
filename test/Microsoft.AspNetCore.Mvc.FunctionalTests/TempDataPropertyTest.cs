// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TempDataPropertyTest : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        protected HttpClient Client { get; }

        public TempDataPropertyTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        [Fact]
        public async Task TempDataPropertyAttribute_RetainsTempDataWithView()
        {
            // Arrange
            var tempDataContent = "Success (from Temp Data)100";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("FullName", "Bob"),
                new KeyValuePair<string, string>("id", "1"),
            };
            var expected = $"{tempDataContent} for person {nameValueCollection[0].Value} with id {nameValueCollection[1].Value}.";
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var redirectResponse = await Client.PostAsync("TempDataProperty/CreateForView", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

            // Act 2
            var response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), redirectResponse));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, body.ToString().Trim());
        }

        [Fact]
        public async Task TempDataPropertyAttribute_RetainsTempDataWithoutView()
        {
            // Arrange
            var tempDataContent = "Success (from Temp Data)100";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("FullName", "Bob"),
                new KeyValuePair<string, string>("id", "1"),
            };
            var expected = $"{tempDataContent} for person {nameValueCollection[0].Value} with id {nameValueCollection[1].Value}.";
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var redirectResponse = await Client.PostAsync("TempDataProperty/Create", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

            // Act 2
            var response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), redirectResponse));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task TempDataPropertyAttribute_TempDataKept()
        {
            // Arrange
            var tempDataContent = "Success (from Temp Data)100";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("FullName", "Bob"),
                new KeyValuePair<string, string>("id", "1"),
            };

            var expected = $"{tempDataContent} for person {nameValueCollection[0].Value} with id {nameValueCollection[1].Value}.";
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await Client.PostAsync("TempDataProperty/CreateNoRedirect", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            response = await Client.SendAsync(GetRequest("TempDataProperty/TempDataKept", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(tempDataContent, body);

            // Act 3
            response = await Client.SendAsync(GetRequest("TempDataProperty/ReadTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            body = await response.Content.ReadAsStringAsync();
            Assert.Equal(tempDataContent, body);
        }

        [Fact]
        public async Task TempDataPropertyAttribute_TempDataNotKept()
        {
            // Arrange
            var tempDataContent = "Success (from Temp Data)100";
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("FullName", "Bob"),
                new KeyValuePair<string, string>("id", "1"),
            };

            var expected = $"{tempDataContent} for person {nameValueCollection[0].Value} with id {nameValueCollection[1].Value}.";
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await Client.PostAsync("TempDataProperty/CreateNoRedirect", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            response = await Client.SendAsync(GetRequest("TempDataProperty/ReadTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(tempDataContent, body);

            // Act 3
            response = await Client.SendAsync(GetRequest("TempDataProperty/ReadTempData", response));

            // Assert 3
            body = await response.Content.ReadAsStringAsync();
            Assert.Empty(body);
        }

        private HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                foreach (var cookie in SetCookieHeaderValue.ParseList(values.ToList()))
                {
                    if (cookie.Expires == null || cookie.Expires >= DateTimeOffset.UtcNow)
                    {
                        request.Headers.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                    }
                }
            }
            return request;
        }
    }
}
