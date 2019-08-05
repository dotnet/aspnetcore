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

        [Fact(Skip = "https://github.com/aspnet/AspNetCore/issues/11459")]
        public override Task SerializableErrorIsReturnedInExpectedFormat() => base.SerializableErrorIsReturnedInExpectedFormat();

        [Fact]
        public override async Task Formatting_StringValueWithUnicodeContent()
        {
            // Act
            var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithUnicodeResult)}");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            Assert.Equal("\"Hello Mr. \\uD83E\\uDD8A\"", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public override Task Formatting_DictionaryType() => base.Formatting_DictionaryType();

        [Fact]
        public override Task Formatting_ProblemDetails() => base.Formatting_ProblemDetails();

        [Fact]
        public override Task Formatting_PolymorphicModel() => base.Formatting_PolymorphicModel();
    }
}