// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using FormatterWebSite.Controllers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase<FormatterWebSite.StartupWithJsonFormatter>
    {
        public SystemTextJsonOutputFormatterTest(MvcTestFixture<FormatterWebSite.StartupWithJsonFormatter> fixture)
            : base(fixture)
        {
        }

        [Fact(Skip = "Insert issue here")]
        public override Task SerializableErrorIsReturnedInExpectedFormat() => base.SerializableErrorIsReturnedInExpectedFormat();

        [Fact]
        public override async Task Formatting_StringValueWithUnicodeContent()
        {
            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithUnicodeResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal("\"Hello Mr. \\ud83e\\udd8a\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public override async Task Formatting_SimpleModel()
        {
            // Arrange
            var expected = "{\"Id\":10,\"Name\":\"Test\",\"StreetName\":\"Some street\"}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.SimpleModelResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public override async Task Formatting_CollectionType()
        {
            // Arrange
            var expected = "[{\"Id\":10,\"Name\":\"TestName\",\"StreetName\":null},{\"Id\":11,\"Name\":\"TestName1\",\"StreetName\":\"Some street\"}]";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.CollectionModelResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact(Skip = "Dictionary serialization does not correctly work.")]
        public override Task Formatting_DictionaryType() => base.Formatting_DictionaryType();

        [Fact(Skip = "Dictionary serialization does not correctly work.")]
        public override Task Formatting_ProblemDetails() => base.Formatting_ProblemDetails();

        [Fact]
        public override async Task Formatting_PolymorphicModel()
        {
            // Arrange
            var expected = "{\"Id\":10,\"Name\":\"test\",\"StreetName\":null}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.PolymorphicResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public override async Task Formatting_LargeObject()
        {
            // Arrange
            var expectedName = "This is long so we can test large objects " + new string('a', 1024 * 65);
            var expected = $"{{\"Id\":10,\"Name\":\"{expectedName}\",\"StreetName\":null}}";

            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.LargeObjectResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }
    }
}