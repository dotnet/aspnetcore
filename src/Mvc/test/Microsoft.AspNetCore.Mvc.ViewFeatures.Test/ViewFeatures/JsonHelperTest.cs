// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class JsonHelperTest
    {
        [Fact]
        public void Serialize_EscapesHtmlByDefault()
        {
            // Arrange
            var settings = new JsonSerializerSettings()
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
            };
            var helper = new JsonHelper(
                new JsonOutputFormatter(settings, ArrayPool<char>.Shared),
                ArrayPool<char>.Shared);
            var obj = new
            {
                HTML = "<b>John Doe</b>"
            };
            var expectedOutput = "{\"HTML\":\"\\u003cb\\u003eJohn Doe\\u003c/b\\u003e\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }

        [Fact]
        public void Serialize_MaintainsSettingsAndEscapesHtml()
        {
            // Arrange
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                },
            };
            var helper = new JsonHelper(
                new JsonOutputFormatter(settings, ArrayPool<char>.Shared),
                ArrayPool<char>.Shared);
            var obj = new
            {
                FullHtml = "<b>John Doe</b>"
            };
            var expectedOutput = "{\"fullHtml\":\"\\u003cb\\u003eJohn Doe\\u003c/b\\u003e\"}";

            // Act
            var result = helper.Serialize(obj);

            // Assert
            var htmlString = Assert.IsType<HtmlString>(result);
            Assert.Equal(expectedOutput, htmlString.ToString());
        }
    }
}
