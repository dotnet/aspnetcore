// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
{
    public class NewtonsoftJsonHelperTest : JsonHelperTestBase
    {
        [Fact]
        public void Serialize_MaintainsSettingsAndEscapesHtml()
        {
            // Arrange
            var options = new MvcNewtonsoftJsonOptions();
            options.SerializerSettings.ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy(),
            };
            var helper = new NewtonsoftJsonHelper(
                Options.Create(options),
                ArrayPool<char>.Shared);
            var obj = new
            {
                FullHtml = "<b>John Doe</b>"
            };
            var expectedOutput = "{\"FullHtml\":\"\\u003cb\\u003eJohn Doe\\u003c/b\\u003e\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public void Serialize_UsesHtmlSafeVersionOfPassedInSettings()
        {
            // Arrange
            var helper = GetJsonHelper();
            var obj = new
            {
                FullHtml = "<b>John Doe</b>"
            };
            var serializerSettings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.Default,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                }
            };

            var expectedOutput = "{\"full_html\":\"\\u003cb\\u003eJohn Doe\\u003c/b\\u003e\"}";

            // Act
            var result = helper.Serialize(obj, serializerSettings);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
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
            var expectedOutput = $"{{\"html\":\"{obj.HTML}\"}}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        protected override IJsonHelper GetJsonHelper()
        {
            var options = new MvcNewtonsoftJsonOptions();
            var helper = new NewtonsoftJsonHelper(
                Options.Create(options),
                ArrayPool<char>.Shared);
            return helper;
        }
    }
}
