// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FormatterWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public abstract class JsonOutputFormatterTestBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
    {
        protected JsonOutputFormatterTestBase(MvcTestFixture<TStartup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<TStartup>();

        public HttpClient Client { get; }

        [Fact]
        public virtual async Task SerializableErrorIsReturnedInExpectedFormat()
        {
            // Arrange
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Employee xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<Id>2</Id><Name>foo</Name></Employee>";

            var expectedOutput = "{\"Id\":[\"The field Id must be between 10 and 100." +
                    "\"],\"Name\":[\"The field Name must be a string or array type with" +
                    " a minimum length of '15'.\"]}";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SerializableError/CreateEmployee");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var actualContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedOutput, actualContent);

            var modelStateErrors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(actualContent);
            Assert.Equal(2, modelStateErrors.Count);

            var errors = Assert.Single(modelStateErrors, kvp => kvp.Key == "Id").Value;

            var error = Assert.Single(errors);
            Assert.Equal("The field Id must be between 10 and 100.", error);

            errors = Assert.Single(modelStateErrors, kvp => kvp.Key == "Name").Value;
            error = Assert.Single(errors);
            Assert.Equal("The field Name must be a string or array type with a minimum length of '15'.", error);
        }

        [Fact]
        public virtual async Task Formatting_IntValue()
        {
            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.IntResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal("2", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_StringValue()
        {
            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal("\"Hello world\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_StringValueWithUnicodeContent()
        {
            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithUnicodeResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal("\"Hello Mr. 🦊\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_SimpleModel()
        {
            // Arrange
            var expected = "{\"id\":10,\"name\":\"Test\",\"streetName\":\"Some street\"}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.SimpleModelResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_CollectionType()
        {
            // Arrange
            var expected = "[{\"id\":10,\"name\":\"TestName\",\"streetName\":null},{\"id\":11,\"name\":\"TestName1\",\"streetName\":\"Some street\"}]";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.CollectionModelResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_DictionaryType()
        {
            // Arrange
            var expected = "{\"SomeKey\":\"Value0\",\"DifferentKey\":\"Value1\",\"Key3\":null}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.DictionaryResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_ProblemDetails()
        {
            using var _ = new ActivityReplacer();

            // Arrange
            var expected = $"{{\"type\":\"https://tools.ietf.org/html/rfc7231#section-6.5.4\",\"title\":\"Not Found\",\"status\":404,\"traceId\":\"{Activity.Current.Id}\"}}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.ProblemDetailsResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public virtual async Task Formatting_PolymorphicModel()
        {
            // Arrange
            var expected = "{\"address\":\"Some address\",\"id\":10,\"name\":\"test\",\"streetName\":null}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.PolymorphicResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }
    }
}