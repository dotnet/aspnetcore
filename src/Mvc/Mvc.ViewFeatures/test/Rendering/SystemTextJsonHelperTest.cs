// Copyright (c) .NET Foundation. All rights reserved
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public class SystemTextJsonHelperTest : JsonHelperTestBase
    {
        protected override IJsonHelper GetJsonHelper() => GetJsonHelper(new JsonOptions());

        private static IJsonHelper GetJsonHelper(JsonOptions options)
        {
            return new SystemTextJsonHelper(Options.Create(options));
        }

        [Fact]
        public override void Serialize_WithNonAsciiChars()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = $"Hello pingüino"
            };
            var expectedOutput = "{\"html\":\"Hello ping\\u00FCino\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public override void Serialize_WithHTMLNonAsciiAndControlChars()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                HTML = "<b>Hello \n pingüino</b>"
            };
            var expectedOutput = "{\"html\":\"\\u003Cb\\u003EHello \\n ping\\u00FCino\\u003C/b\\u003E\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public void Serialize_UsesOptionsConfiguredInTheProvider()
        {
            // Arrange
            // This should use property-casing and indentation, but the result should be HTML-safe
            var options = new JsonOptions
            {
                JsonSerializerOptions =
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = null,
                    WriteIndented = true,
                }
            };
            var helper = GetJsonHelper(options);
            var obj = new
            {
                HTML = "<b>John</b>"
            };
            var expectedOutput =
@"{
  ""HTML"": ""\u003Cb\u003EJohn\u003C/b\u003E""
}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString(), ignoreLineEndingDifferences: true);
        }
    }
}
