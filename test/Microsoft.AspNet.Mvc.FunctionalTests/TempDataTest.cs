// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TempDataTest
    {
        private const string SiteName = nameof(TempDataWebSite);
        private readonly Action<IApplicationBuilder> _app = new TempDataWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new TempDataWebSite.Startup().ConfigureServices;

        [Fact]
        public async Task TempData_PersistsJustForNextRequest()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await client.PostAsync("/Home/SetTempData", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            response = await client.SendAsync(GetRequest("Home/GetTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);

            // Act 3
            response = await client.SendAsync(GetRequest("Home/GetTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ViewRendersTempData()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.PostAsync("http://localhost/Home/DisplayTempData", content);
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }

        [Fact]
        public async Task Redirect_RetainsTempData_EvenIfAccessed()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await client.PostAsync("/Home/SetTempData", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            var redirectResponse = await client.SendAsync(GetRequest("/Home/GetTempDataAndRedirect", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

            // Act 3
            response = await client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), response));

            // Assert 3
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }

        [Fact]
        public async Task Peek_RetainsTempData()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await client.PostAsync("/Home/SetTempData", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            var peekResponse = await client.SendAsync(GetRequest("/Home/PeekTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, peekResponse.StatusCode);
            var body = await peekResponse.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);

            // Act 3
            var getResponse = await client.SendAsync(GetRequest("/Home/GetTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            body = await getResponse.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }

        private HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            IEnumerable<string> values;
            if (response.Headers.TryGetValues("Set-Cookie", out values))
            {
                var cookie = SetCookieHeaderValue.ParseList(values.ToList()).First();
                request.Headers.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
            }

            return request;
        }
    }
}