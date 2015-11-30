// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TempDataTest : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public TempDataTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task TempData_PersistsJustForNextRequest()
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

            // Act 2
            response = await Client.SendAsync(GetRequest("TempData/GetTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);

            // Act 3
            response = await Client.SendAsync(GetRequest("TempData/GetTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ViewRendersTempData()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await Client.PostAsync("http://localhost/TempData/DisplayTempData", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Redirect_RetainsTempData_EvenIfAccessed()
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
        }

        [Fact]
        public async Task Peek_RetainsTempData()
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

            // Act 2
            var peekResponse = await Client.SendAsync(GetRequest("/TempData/PeekTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, peekResponse.StatusCode);
            var body = await peekResponse.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);

            // Act 3
            var getResponse = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            body = await getResponse.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/21
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task TempData_ValidTypes_RoundTripProperly()
        {
            // Arrange
            var testGuid = Guid.NewGuid();
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
                new KeyValuePair<string, string>("intValue", "10"),
                new KeyValuePair<string, string>("listValues", "Foo1"),
                new KeyValuePair<string, string>("listValues", "Foo2"),
                new KeyValuePair<string, string>("listValues", "Foo3"),
                new KeyValuePair<string, string>("datetimeValue", "10/10/2010"),
                new KeyValuePair<string, string>("guidValue", testGuid.ToString()),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var redirectResponse = await Client.PostAsync("/TempData/SetTempDataMultiple", content);

            // Assert 1
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

            // Act 2
            var response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), redirectResponse));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal($"Foo 10 3 10/10/2010 00:00:00 {testGuid.ToString()}", body);
        }

        [Fact]
        public async Task TempData_InvalidType_Throws()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Client.PostAsync("/TempData/SetTempDataInvalidType", content);
            });
            Assert.Equal("The '" + typeof(SessionStateTempDataProvider).FullName + "' cannot serialize an object of type '" +
                typeof(BasicWebSite.Controllers.TempDataController.NonSerializableType).FullName + "' to session state.", exception.Message);
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