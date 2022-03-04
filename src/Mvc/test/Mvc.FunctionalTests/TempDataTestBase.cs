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
    public abstract class TempDataTestBase
    {
        protected abstract HttpClient Client { get; }

        [Fact]
        public async Task PersistsJustForNextRequest()
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
            response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);

            // Act 3
            response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

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
            var response = await Client.PostAsync("/TempData/DisplayTempData", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Foo", body);
        }

        [Fact]
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

        [Fact]
        public async Task ValidTypes_RoundTripProperly()
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
        public async Task ResponseWrite_DoesNotCrashSaveTempDataFilter()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Name", "Jordan"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act, checking it didn't throw
            var response = await Client.GetAsync("/TempData/SetTempDataResponseWrite");
        }

        [Fact]
        public async Task SetInActionResultExecution_AvailableForNextRequest()
        {
            // Arrange
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Name", "Jordan"),
            };
            var content = new FormUrlEncodedContent(nameValueCollection);

            // Act 1
            var response = await Client.GetAsync("/TempData/SetTempDataInActionResult");

            // Assert 1
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act 2
            response = await Client.SendAsync(GetRequest("/TempData/GetTempDataSetInActionResult", response));

            // Assert 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Michael", body);

            // Act 3
            response = await Client.SendAsync(GetRequest("/TempData/GetTempDataSetInActionResult", response));

            // Assert 3
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task SaveTempDataFilter_DoesNotSaveTempData_OnUnhandledException()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/TempData/UnhandledExceptionAndSettingTempData");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Exception from action UnhandledExceptionAndSettingTempData", responseBody);

            // Arrange & Act
            response = await Client.GetAsync("/TempData/UnhandledExceptionAndGetTempData");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task SaveTempDataFilter_DoesNotSaveTempData_OnHandledExceptions()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/TempData/UnhandledExceptionAndSettingTempData?handleException=true");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Exception was handled in TestExceptionFilter", responseBody);

            // Arrange & Act
            response = await Client.GetAsync("/TempData/UnhandledExceptionAndGetTempData");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        public HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
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
            return request;
        }
    }
}
