// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.TagHelpers.Internal;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Logging;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class ScriptTagHelperTest
    {
        [Theory]
        [InlineData("~/blank.js")]
        [InlineData(null)]
        public async Task RunsWhenRequiredAttributesArePresent(string srcValue)
        {
            // Arrange
            var attributes = new Dictionary<string, object>
            {
                ["asp-fallback-src"] = "http://www.example.com/blank.js",
                ["asp-fallback-test"] = "isavailable()",
            };

            if (srcValue != null)
            {
                attributes.Add("src", srcValue);
            }

            var context = MakeTagHelperContext(attributes);

            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper()
            {
                Logger = logger,
                FallbackSrc = "http://www.example.com/blank.js",
                FallbackTestExpression = "isavailable()",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Null(output.TagName);
            Assert.NotNull(output.Content);
            Assert.True(output.ContentSet);
            Assert.Empty(logger.Logged);
        }

        public static TheoryData MissingAttributeDataSet
        {
            get
            {
                return new TheoryData<Dictionary<string, object>, ScriptTagHelper, string>
                {
                    {
                        new Dictionary<string, object> // the attributes provided
                        {
                            ["asp-fallback-src"] =  "http://www.example.com/blank.js",
                        },
                        new ScriptTagHelper() // the tag helper
                        {
                            FallbackTestExpression = "isavailable()",
                        },
                        "asp-fallback-test" // missing attribute
                    },

                    {
                        new Dictionary<string, object> // the attributes provided
                        {
                            ["asp-fallback-test"] = "isavailable()",
                        },
                        new ScriptTagHelper() // the tag helper
                        {
                            FallbackTestExpression = "http://www.example.com/blank.js",
                        },
                        "asp-fallback-src" // missing attribute
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MissingAttributeDataSet))]
        public async Task DoesNotRunWhenARequiredAttributeIsMissing(
            Dictionary<string, object> attributes,
            ScriptTagHelper helper,
            string attributeMissing)
        {
            // Arrange
            Assert.Single(attributes);

            var context = MakeTagHelperContext(attributes);

            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            helper.Logger = logger;

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.ContentSet);
        }

        [Fact]
        public async Task DoesNotRunWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var context = MakeTagHelperContext();
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper
            {
                Logger = logger,
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.ContentSet);
        }

        [Theory]
        [MemberData(nameof(MissingAttributeDataSet))]
        public async Task LogsWhenARequiredAttributeIsMissing(
            Dictionary<string, object> attributes,
            ScriptTagHelper helper,
            string attributeMissing)
        {
            // Arrange
            Assert.Single(attributes);

            var context = MakeTagHelperContext(attributes);

            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            helper.Logger = logger;

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.ContentSet);

            Assert.Equal(2, logger.Logged.Count);

            Assert.Equal(LogLevel.Warning, logger.Logged[0].LogLevel);
            Assert.IsType<MissingAttributeLoggerStructure>(logger.Logged[0].State);

            var loggerData0 = (MissingAttributeLoggerStructure)logger.Logged[0].State;
            Assert.Single(loggerData0.MissingAttributes);
            Assert.Equal(attributeMissing, loggerData0.MissingAttributes.Single());

            Assert.Equal(LogLevel.Verbose, logger.Logged[1].LogLevel);
            Assert.IsAssignableFrom<ILoggerStructure>(logger.Logged[1].State);
            Assert.StartsWith("Skipping processing for ScriptTagHelper",
                ((ILoggerStructure)logger.Logged[1].State).Format());
        }

        [Fact]
        public async Task LogsWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var context = MakeTagHelperContext();
            var output = MakeTagHelperOutput("script");
            var logger = CreateLogger();

            var helper = new ScriptTagHelper
            {
                Logger = logger,
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("script", output.TagName);
            Assert.False(output.ContentSet);

            Assert.Single(logger.Logged);

            Assert.Equal(LogLevel.Verbose, logger.Logged[0].LogLevel);
            Assert.IsAssignableFrom<ILoggerStructure>(logger.Logged[0].State);
            Assert.StartsWith("Skipping processing for ScriptTagHelper",
                ((ILoggerStructure)logger.Logged[0].State).Format());
        }

        [Fact]
        public async Task PreservesOrderOfSourceAttributesWhenRun()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    ["data-extra"] = "something",
                    ["src"] = "/blank.js",
                    ["data-more"] = "else",
                    ["asp-fallback-src"] = "http://www.example.com/blank.js",
                    ["asp-fallback-test"] = "isavailable()",
                });

            var output = MakeTagHelperOutput("link",
                attributes: new Dictionary<string, string>
                {
                    ["data-extra"] = "something",
                    ["src"] = "/blank.js",
                    ["data-more"] = "else",
                });

            var logger = CreateLogger();

            var helper = new ScriptTagHelper
            {
                Logger = logger,
                FallbackSrc = "~/blank.js",
                FallbackTestExpression = "http://www.example.com/blank.js",
            };

            // Act
            await helper.ProcessAsync(context, output);

            // Assert
            Assert.StartsWith("<script data-extra=\"something\" src=\"/blank.js\" data-more=\"else\"", output.Content);
            Assert.Empty(logger.Logged);
        }

        private TagHelperContext MakeTagHelperContext(
            IDictionary<string, object> attributes = null,
            string content = null)
        {
            attributes = attributes ?? new Dictionary<string, object>();

            return new TagHelperContext(attributes, Guid.NewGuid().ToString("N"), () => Task.FromResult(content));
        }

        private TagHelperOutput MakeTagHelperOutput(string tagName, IDictionary<string, string> attributes = null)
        {
            attributes = attributes ?? new Dictionary<string, string>();

            return new TagHelperOutput(tagName, attributes);
        }

        private TagHelperLogger<ScriptTagHelper> CreateLogger()
        {
            return new TagHelperLogger<ScriptTagHelper>();
        }
    }
}