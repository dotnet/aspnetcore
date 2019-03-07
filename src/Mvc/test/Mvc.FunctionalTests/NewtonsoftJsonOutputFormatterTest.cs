// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class NewtonsoftJsonOutputFormatterTest : JsonOutputFormatterTestBase<FormatterWebSite.Startup>
    {
        public NewtonsoftJsonOutputFormatterTest(MvcTestFixture<FormatterWebSite.Startup> fixture)
            : base(fixture)
        {
        }

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
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }
    }
}