// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FormatterWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public abstract class JsonInputFormatterTestBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
    {
        protected JsonInputFormatterTestBase(MvcTestFixture<TStartup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<TStartup>();

        public HttpClient Client { get; }

        [Theory]
        [InlineData("application/json")]
        [InlineData("text/json")]
        public async Task JsonInputFormatter_IsSelectedForJsonRequest(string requestContentType)
        {
            // Arrange
            var sampleInputInt = 10;
            var input = "{\"sampleInt\":10}";
            var content = new StringContent(input, Encoding.UTF8, requestContentType);

            // Act
            var response = await Client.PostAsync("http://localhost/Home/Index", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sampleInputInt.ToString(), await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("application/json", "{\"sampleInt\":10}", 10)]
        [InlineData("application/json", "{}", 0)]
        public async Task JsonInputFormatter_IsModelStateValid_ForValidContentType(
            string requestContentType,
            string jsonInput,
            int expectedSampleIntValue)
        {
            // Arrange
            var content = new StringContent(jsonInput, Encoding.UTF8, requestContentType);

            // Act
            var response = await Client.PostAsync("http://localhost/JsonFormatter/ReturnInput/", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedSampleIntValue.ToString(), responseBody);
        }

        [Theory]
        [InlineData("\"I'm a JSON string!\"")]
        [InlineData("true")]
        [InlineData("\"\"")] // Empty string
        public virtual async Task JsonInputFormatter_ReturnsDefaultValue_ForValueTypes(string input)
        {
            // Arrange
            var content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("http://localhost/JsonFormatter/ValueTypeAsBody/", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("0", responseBody);
        }

        [Fact]
        public async Task JsonInputFormatter_ReadsPrimitiveTypes()
        {
            // Arrange
            var expected = "1773";
            var content = new StringContent(expected, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("http://localhost/JsonFormatter/ValueTypeAsBody/", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, responseBody);
        }

        [Fact]
        public async Task JsonInputFormatter_RoundtripsPocoModel()
        {
            // Arrange
            var expected = new JsonFormatterController.SimpleModel()
            {
                Id = 18,
                Name = "James",
                StreetName = "JnK",
            };

            // Act
            var response = await Client.PostAsJsonAsync("http://localhost/JsonFormatter/RoundtripSimpleModel/", expected);
            var actual = await response.Content.ReadAsAsync<JsonFormatterController.SimpleModel>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.StreetName, actual.StreetName);
        }

        [Fact]
        public async Task JsonInputFormatter_Returns415UnsupportedMediaType_ForEmptyContentType()
        {
            // Arrange
            var jsonInput = "{\"sampleInt\":10}";
            var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");
            content.Headers.Clear();

            // Act
            var response = await Client.PostAsync("http://localhost/JsonFormatter/ReturnInput/", content);

            // Assert
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }

        [Theory]
        [InlineData("application/json", "{\"sampleInt\":10}", 10)]
        [InlineData("application/json", "{}", 0)]
        public async Task JsonInputFormatter_IsModelStateValid_ForTransferEncodingChunk(
            string requestContentType,
            string jsonInput,
            int expectedSampleIntValue)
        {
            // Arrange
            var content = new StringContent(jsonInput, Encoding.UTF8, requestContentType);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/JsonFormatter/ReturnInput/");
            request.Headers.TransferEncodingChunked = true;
            request.Content = content;

            // Act
            var response = await Client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedSampleIntValue.ToString(), responseBody);
        }
    }
}