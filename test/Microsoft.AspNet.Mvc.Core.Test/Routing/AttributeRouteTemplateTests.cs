// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.Routing
{
    public class AttributeRouteTemplateTests
    {
        [Theory]
        [InlineData(null, null, null)]
        [InlineData("", null, "")]
        [InlineData(null, "", "")]
        [InlineData("/", null, "")]
        [InlineData(null, "/", "")]
        [InlineData("/", "", "")]
        [InlineData("", "/", "")]
        [InlineData("/", "/", "")]
        [InlineData("/", "/", "")]
        [InlineData("~/", null, "")]
        [InlineData("~/", "", "")]
        [InlineData("~/", "/", "")]
        [InlineData("~/", "~/", "")]
        [InlineData(null, "~/", "")]
        [InlineData("", "~/", "")]
        [InlineData("/", "~/", "")]
        public void Combine_EmptyTemplates(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }

        [Theory]
        [InlineData("home", null, "home")]
        [InlineData("home", "", "home")]
        [InlineData("/home/", "/", "")]
        [InlineData("/home/", "~/", "")]
        [InlineData(null, "GetEmployees", "GetEmployees")]
        [InlineData("/", "GetEmployees", "GetEmployees")]
        [InlineData("~/", "Blog/Index/", "Blog/Index")]
        [InlineData("", "/GetEmployees/{id}/", "GetEmployees/{id}")]
        [InlineData("~/home", null, "home")]
        [InlineData("~/home", "", "home")]
        [InlineData("~/home", "/", "")]
        [InlineData(null, "~/home", "home")]
        [InlineData("", "~/home", "home")]
        [InlineData("", "~/home/", "home")]
        [InlineData("/", "~/home", "home")]
        public void Combine_OneTemplateHasValue(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }

        [Theory]
        [InlineData("home", "About", "home/About")]
        [InlineData("home/", "/About", "About")]
        [InlineData("home/", "/About/", "About")]
        [InlineData("/home/{action}", "{id}", "home/{action}/{id}")]
        [InlineData("home", "~/index", "index")]
        [InlineData("home", "~/index/", "index")]
        public void Combine_BothTemplatesHasValue(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }

        [Theory]
        [InlineData("~~/", null, "~~")]
        [InlineData("~~/", "", "~~")]
        [InlineData("~~/", "//", "//")]
        [InlineData("~~/", "~~/", "~~/~~")]
        [InlineData("~~/", "home", "~~/home")]
        [InlineData("~~/", "home/", "~~/home")]
        [InlineData("//", null, "//")]
        [InlineData("//", "", "//")]
        [InlineData("//", "//", "//")]
        [InlineData("//", "~~/", "/~~")]
        [InlineData("//", "home", "/home")]
        [InlineData("//", "home/", "/home")]
        [InlineData("////", null, "//")]
        [InlineData("~~//", null, "~~/")]
        public void Combine_InvalidTemplates(string left, string right, string expected)
        {
            // Arrange & Act
            var combined = AttributeRouteTemplate.Combine(left, right);

            // Assert
            Assert.Equal(expected, combined);
        }

        public static IEnumerable<object[]> ReplaceTokens_ValueValuesData
        {
            get
            {
                yield return new object[]
                {
                    "[controller]/[action]",
                    new { controller = "Home", action = "Index" },
                    "Home/Index"
                };

                yield return new object[]
                {
                    "[controller]",
                    new { controller = "Home", action = "Index" },
                    "Home"
                };

                yield return new object[]
                {
                    "[controller][[",
                    new { controller = "Home", action = "Index" },
                    "Home["
                };

                yield return new object[]
                {
                    "[coNTroller]",
                    new { contrOLler = "Home", action = "Index" },
                    "Home"
                };

                yield return new object[]
                {
                    "thisisSomeText[action]",
                    new { controller = "Home", action = "Index" },
                    "thisisSomeTextIndex"
                };

                yield return new object[]
                {
                    "[[-]][[/[[controller]]",
                    new { controller = "Home", action = "Index" },
                    "[-][/[controller]"
                };

                yield return new object[]
                {
                    "[contr[[oller]/[act]]ion]",
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "contr[oller", "Home" },
                        { "act]ion", "Index" }
                    },
                    "Home/Index"
                };

                yield return new object[]
                {
                    "[controller][action]",
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                    "HomeIndex"
                };

                yield return new object[]
                {
                    "[contr}oller]/[act{ion]/{id}",
                    new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "contr}oller", "Home" },
                        { "act{ion", "Index" }
                    },
                    "Home/Index/{id}"
                };
            }
        }

        [Theory]
        [MemberData("ReplaceTokens_ValueValuesData")]
        public void ReplaceTokens_ValidValues(string template, object values, string expected)
        {
            // Arrange
            var valuesDictionary = values as IDictionary<string, object>;
            if (valuesDictionary == null)
            {
                valuesDictionary = new RouteValueDictionary(values);
            }

            // Act
            var result = AttributeRouteTemplate.ReplaceTokens(template, valuesDictionary);

            // Assert
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> ReplaceTokens_InvalidFormatValuesData
        {
            get
            {
                yield return new object[]
                {
                    "[",
                    new { },
                    "A replacement token is not closed."
                };

                yield return new object[]
                {
                    "text]",
                    new { },
                    "Token delimiters ('[', ']') are imbalanced.",
                };

                yield return new object[]
                {
                    "text]morecooltext",
                    new { },
                    "Token delimiters ('[', ']') are imbalanced.",
                };

                yield return new object[]
                {
                    "[action",
                    new { },
                    "A replacement token is not closed.",
                };

                yield return new object[]
                {
                    "[action]]][",
                    new RouteValueDictionary()
                    {
                        { "action]", "Index" }
                    },
                    "A replacement token is not closed.",
                };

                yield return new object[]
                {
                    "[action]]",
                    new { },
                    "A replacement token is not closed."
                };

                yield return new object[]
                {
                    "[ac[tion]",
                    new { },
                    "An unescaped '[' token is not allowed inside of a replacement token. Use '[[' to escape."
                };

                yield return new object[]
                {
                    "[]",
                    new { },
                    "An empty replacement token ('[]') is not allowed.",
                };
            }
        }

        [Theory]
        [MemberData("ReplaceTokens_InvalidFormatValuesData")]
        public void ReplaceTokens_InvalidFormat(string template, object values, string reason)
        {
            // Arrange
            var valuesDictionary = values as IDictionary<string, object>;
            if (valuesDictionary == null)
            {
                valuesDictionary = new RouteValueDictionary(values);
            }

            var expected = string.Format(
                "The route template '{0}' has invalid syntax. {1}",
                template,
                reason);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => { AttributeRouteTemplate.ReplaceTokens(template, valuesDictionary); });

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void ReplaceTokens_UnknownValue()
        {
            // Arrange
            var template = "[area]/[controller]/[action2]";
            var values = new RouteValueDictionary()
            {
                { "area", "Help" },
                { "controller", "Admin" },
                { "action", "SeeUsers" },
            };

            var expected =
                "While processing template '[area]/[controller]/[action2]', " +
                "a replacement value for the token 'action2' could not be found. " +
                "Available tokens: 'area, controller, action'.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(
                () => { AttributeRouteTemplate.ReplaceTokens(template, values); });

            // Assert
            Assert.Equal(expected, ex.Message);
        }
    }
}