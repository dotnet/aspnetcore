// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JsonPatchSample.Web.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class JsonPatchSampleTest : IClassFixture<MvcTestFixture<JsonPatchSample.Web.Startup>>
    {
        public JsonPatchSampleTest(MvcTestFixture<JsonPatchSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithoutModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch")]
        public async Task JsonPatch_ValidAddOperation_Success(string url)
        {
            // Arrange
            var input = "[{ \"op\": \"add\", " +
                "\"path\": \"Orders/2\", " +
               "\"value\": { \"OrderName\": \"Name2\" }}]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(body);
            Assert.Equal("Name2", customer.Orders[2].OrderName);
        }

        [Theory]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithoutModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch")]
        public async Task JsonPatch_ValidReplaceOperation_Success(string url)
        {
            // Arrange
            var input = "[{ \"op\": \"replace\", " +
                "\"path\": \"Orders/0/OrderName\", " +
               "\"value\": \"ReplacedOrder\" }]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(body);
            Assert.Equal("ReplacedOrder", customer.Orders[0].OrderName);
        }

        [Theory]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithoutModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch")]
        public async Task JsonPatch_ValidCopyOperation_Success(string url)
        {
            // Arrange
            var input = "[{ \"op\": \"copy\", " +
                "\"path\": \"Orders/1/OrderName\", " +
               "\"from\": \"Orders/0/OrderName\"}]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(body);
            Assert.Equal("Order0", customer.Orders[1].OrderName);
        }

        [Theory]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithoutModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch")]
        public async Task JsonPatch_ValidMoveOperation_Success(string url)
        {
            // Arrange
            var input = "[{ \"op\": \"move\", " +
                "\"path\": \"Orders/1/OrderName\", " +
               "\"from\": \"Orders/0/OrderName\"}]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();

            var customer = JsonConvert.DeserializeObject<Customer>(body);
            Assert.Equal("Order0", customer.Orders[1].OrderName);
            Assert.Null(customer.Orders[0].OrderName);
        }

        [Theory]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithoutModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch")]
        public async Task JsonPatch_ValidRemoveOperation_Success(string url)
        {
            // Arrange
            var input = "[{ \"op\": \"remove\", " +
                "\"path\": \"Orders/1/OrderName\"}]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(body);
            Assert.Null(customer.Orders[1].OrderName);
        }

        [Theory]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithoutModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelState")]
        [InlineData("http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch")]
        public async Task JsonPatch_MultipleValidOperations_Success(string url)
        {
            // Arrange
            var input = "[{ \"op\": \"add\", "+
                "\"path\": \"Orders/2\", " +
               "\"value\": { \"OrderName\": \"Name2\" }}, " +
               "{\"op\": \"copy\", " +
               "\"from\": \"Orders/2\", " +
                "\"path\": \"Orders/3\" }, " +
                "{\"op\": \"replace\", " +
                "\"path\": \"Orders/2/OrderName\", " +
                "\"value\": \"ReplacedName\" }]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(body);
            Assert.Equal("ReplacedName", customer.Orders[2].OrderName);
            Assert.Equal("Name2", customer.Orders[3].OrderName);
        }

        public static IEnumerable<object[]> InvalidJsonPatchData
        {
            get
            {
                return new[]
                {
                    new object[] {
                        "http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch",
                        "[{ \"op\": \"add\", " +
                        "\"path\": \"Orders/5\", " +
                        "\"value\": { \"OrderName\": \"Name5\" }}]",
                        "{\"Patch.Customer\":[\"For operation 'add' on array property at path " +
                        "'Orders/5', the index is larger than the array size.\"]}"
                    },
                    new object[] {
                        "http://localhost/jsonpatch/JsonPatchWithModelState",
                        "[{ \"op\": \"add\", " +
                        "\"path\": \"Orders/5\", " +
                        "\"value\": { \"OrderName\": \"Name5\" }}]",
                        "{\"Customer\":[\"For operation 'add' on array property at path " +
                        "'Orders/5', the index is larger than the array size.\"]}"
                    },
                    new object[] {
                        "http://localhost/jsonpatch/JsonPatchWithModelStateAndPrefix?prefix=Patch",
                        "[{ \"op\": \"add\", " +
                        "\"path\": \"Orders/2\", " +
                        "\"value\": { \"OrderName\": \"Name2\" }}, " +
                        "{\"op\": \"copy\", " +
                        "\"from\": \"Orders/4\", " +
                        "\"path\": \"Orders/3\" }, " +
                        "{\"op\": \"replace\", " +
                        "\"path\": \"Orders/2/OrderName\", " +
                        "\"value\": \"ReplacedName\" }]",
                        "{\"Patch.Customer\":[\"For operation 'copy' on array property at path " +
                        "'Orders/4', the index is larger than the array size.\"]}"
                    },
                    new object[] {
                        "http://localhost/jsonpatch/JsonPatchWithModelState",
                        "[{ \"op\": \"add\", " +
                        "\"path\": \"Orders/2\", " +
                        "\"value\": { \"OrderName\": \"Name2\" }}, " +
                        "{\"op\": \"copy\", " +
                        "\"from\": \"Orders/4\", " +
                        "\"path\": \"Orders/3\" }, " +
                        "{\"op\": \"replace\", " +
                        "\"path\": \"Orders/2/OrderName\", " +
                        "\"value\": \"ReplacedName\" }]",
                        "{\"Customer\":[\"For operation 'copy' on array property at path " +
                        "'Orders/4', the index is larger than the array size.\"]}"
                    }
                };
            }
        }

        [Theory, MemberData("InvalidJsonPatchData")]
        public async Task JsonPatch_InvalidOperations_failure(string url, string input, string errorMessage)
        {
            // Arrange
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri(url)
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(errorMessage, body);
        }

        [Fact]
        public async Task JsonPatch_InvalidData_FormatterErrorInModelState_Failure()
        {
            // Arrange
            var input = "{ \"op\": \"add\", " +
                "\"path\": \"Orders/2\", " +
               "\"value\": { \"OrderName\": \"Name2\" }}";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri("http://localhost/jsonpatch/JsonPatchWithModelState")
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("{\"\":[\"The input was not valid.\"]}", body);
        }

        [Fact]
        public async Task JsonPatch_JsonConverterOnProperty_Success()
        {
            // Arrange
            var input = "[{ \"op\": \"add\", " +
                "\"path\": \"Orders/2\", " +
               "\"value\": { \"OrderType\": \"Type2\" }}]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri("http://localhost/jsonpatch/JsonPatchWithoutModelState")
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            dynamic d = JObject.Parse(body);
            Assert.Equal("OrderTypeSetInConverter", (string)d.Orders[2].OrderType);
        }

        [Fact]
        public async Task JsonPatch_JsonConverterOnClass_Success()
        {
            // Arrange
            var input = "[{ \"op\": \"add\", " +
                "\"path\": \"ProductCategory\", " +
               "\"value\": { \"CategoryName\": \"Name2\" }}]";
            var request = new HttpRequestMessage
            {
                Content = new StringContent(input, Encoding.UTF8, "application/json-patch+json"),
                Method = new HttpMethod("PATCH"),
                RequestUri = new Uri("http://localhost/jsonpatch/JsonPatchForProduct")
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            dynamic d = JObject.Parse(body);
            Assert.Equal("CategorySetInConverter", (string)d.ProductCategory.CategoryName);

        }
    }
}