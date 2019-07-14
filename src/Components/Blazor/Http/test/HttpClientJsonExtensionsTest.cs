// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class HttpClientJsonExtensionsTest
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        const string TestUri = "http://example.com/some/uri";

        [Fact]
        public async Task GetJson_Success()
        {
            // Arrange
            var httpClient = new HttpClient(new TestHttpMessageHandler(req =>
            {
                Assert.Equal(TestUri, req.RequestUri.AbsoluteUri);
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, new Person
                {
                    Name = "Abc",
                    Age = 123
                }));
            }));

            // Act
            var result = await httpClient.GetJsonAsync<Person>(TestUri);

            // Assert
            Assert.Equal("Abc", result.Name);
            Assert.Equal(123, result.Age);
        }

        [Fact]
        public async Task GetJson_Failure()
        {
            // Arrange
            var httpClient = new HttpClient(new TestHttpMessageHandler(req =>
            {
                Assert.Equal(TestUri, req.RequestUri.AbsoluteUri);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }));

            // Act/Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(
                () => httpClient.GetJsonAsync<Person>(TestUri));
            Assert.Contains("404 (Not Found)", ex.Message);
        }

        [Theory]
        [InlineData("Put")]
        [InlineData("Post")]
        [InlineData("Patch")]
        [InlineData("Delete")]
        [InlineData("MyArtificialMethod")]
        public async Task SendJson_Success(string httpMethodString)
        {
            var httpMethod = new HttpMethod(httpMethodString);
            var requestContent = new { MyProp = true, OtherProp = "Hello" };

            // Arrange
            var httpClient = new HttpClient(new TestHttpMessageHandler(async req =>
            {
                Assert.Equal(httpMethod, req.Method);
                Assert.Equal(TestUri, req.RequestUri.AbsoluteUri);
                Assert.Equal(JsonSerializer.Serialize(requestContent, _jsonSerializerOptions), await ((StringContent)req.Content).ReadAsStringAsync());
                return CreateJsonResponse(HttpStatusCode.OK, new Person
                {
                    Name = "Abc",
                    Age = 123
                });
            }));

            // Act
            var result = await Send(httpClient, httpMethodString, requestContent);

            // Assert
            Assert.Equal("Abc", result.Name);
            Assert.Equal(123, result.Age);
        }

        [Fact]
        public async Task ReadAsJsonAsync_ReadsCamelCasedJson()
        {
            var input = "{\"name\": \"TestPerson\", \"age\": 23 }";

            // Arrange
            var httpClient = new HttpClient(new TestHttpMessageHandler(req =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(input)
                });
            }));

            // Act
            var result = await httpClient.GetJsonAsync<Person>(TestUri);

            // Assert
            Assert.Equal("TestPerson", result.Name);
            Assert.Equal(23, result.Age);
        }

        [Fact]
        public async Task ReadAsJsonAsync_ReadsPascalCasedJson()
        {
            var input = "{\"Name\": \"TestPerson\", \"Age\": 23 }";

            // Arrange
            var httpClient = new HttpClient(new TestHttpMessageHandler(req =>
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(input)
                });
            }));

            // Act
            var result = await httpClient.GetJsonAsync<Person>(TestUri);

            // Assert
            Assert.Equal("TestPerson", result.Name);
            Assert.Equal(23, result.Age);
        }

        [Theory]
        [InlineData("Put")]
        [InlineData("Post")]
        [InlineData("Patch")]
        [InlineData("Delete")]
        [InlineData("MyArtificialMethod")]
        public async Task SendJson_Failure(string httpMethodString)
        {
            var httpMethod = new HttpMethod(httpMethodString);
            var requestContent = new { MyProp = true, OtherProp = "Hello" };

            // Arrange
            var httpClient = new HttpClient(new TestHttpMessageHandler(async req =>
            {
                Assert.Equal(httpMethod, req.Method);
                Assert.Equal(TestUri, req.RequestUri.AbsoluteUri);
                Assert.Equal(JsonSerializer.Serialize(requestContent, _jsonSerializerOptions), await ((StringContent)req.Content).ReadAsStringAsync());
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }));

            // Act/Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(
                () => Send(httpClient, httpMethodString, requestContent));
            Assert.Contains("502 (Bad Gateway)", ex.Message);
        }

        HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, object content)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(content, _jsonSerializerOptions))
            };
        }

        Task<Person> Send(HttpClient httpClient, string httpMethodString, object requestContent)
        {
            // For methods with convenience overloads, show those overloads work
            switch (httpMethodString)
            {
                case "post":
                    return httpClient.PostJsonAsync<Person>(TestUri, requestContent);
                case "put":
                    return httpClient.PutJsonAsync<Person>(TestUri, requestContent);
                default:
                    return httpClient.SendJsonAsync<Person>(new HttpMethod(httpMethodString), TestUri, requestContent);
            }
        }

        class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sendDelegate;

            public TestHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendDelegate)
            {
                _sendDelegate = sendDelegate;
            }

            protected override void Dispose(bool disposing)
                => base.Dispose(disposing);

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
                => _sendDelegate(request);
        }
    }
}
