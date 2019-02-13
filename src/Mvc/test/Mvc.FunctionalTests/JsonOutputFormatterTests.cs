// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Testing.xunit;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class JsonOutputFormatterTests : IClassFixture<MvcTestFixture<FormatterWebSite.Startup>>
    {
        public JsonOutputFormatterTests(MvcTestFixture<FormatterWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task JsonOutputFormatter_ReturnsIndentedJson()
        {
            // Arrange
            var user = new FormatterWebSite.User()
            {
                Id = 1,
                Alias = "john",
                description = "Administrator",
                Designation = "Administrator",
                Name = "John Williams"
            };

            var serializerSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            serializerSettings.Formatting = Formatting.Indented;
            var expectedBody = JsonConvert.SerializeObject(user, serializerSettings);

            // Act
            var response = await Client.GetAsync("http://localhost/JsonFormatter/ReturnsIndentedJson");

            // Assert
            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task SerializableErrorIsReturnedInExpectedFormat()
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
    }
}