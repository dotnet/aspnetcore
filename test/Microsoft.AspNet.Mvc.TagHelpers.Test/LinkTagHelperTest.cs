// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Test
{
    public class LinkTagHelperTest
    {
        [Fact]
        public void RunsWhenRequiredAttributesArePresent()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    { "asp-fallback-href", "test.css" },
                    { "asp-fallback-test-class", "hidden" },
                    { "asp-fallback-test-property", "visible" },
                    { "asp-fallback-test-value", "hidden" },
                });
            var output = MakeTagHelperOutput("link");
            var loggerFactory = new Mock<ILoggerFactory>();

            // Act
            var helper = new LinkTagHelper
            {
                LoggerFactory = loggerFactory.Object,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visible",
                FallbackTestValue = "hidden"
            };
            helper.Process(context, output);

            // Assert
            Assert.Null(output.TagName);
            Assert.NotNull(output.Content);
            Assert.True(output.ContentSet);
        }
        
        [Fact]
        public void PreservesOrderOfSourceAttributesWhenRun()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    { "rel", "stylesheet"},
                    { "data-extra", "something"},
                    { "href", "test.css"},
                    { "asp-fallback-href", "test.css" },
                    { "asp-fallback-test-class", "hidden" },
                    { "asp-fallback-test-property", "visible" },
                    { "asp-fallback-test-value", "hidden" }
                });
            var output = MakeTagHelperOutput("link",
                attributes: new Dictionary<string, string>
                {
                    { "rel", "stylesheet"},
                    { "data-extra", "something"},
                    { "href", "test.css"}
                });
            var loggerFactory = new Mock<ILoggerFactory>();

            // Act
            var helper = new LinkTagHelper
            {
                LoggerFactory = loggerFactory.Object,
                FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visible",
                FallbackTestValue = "hidden"
            };
            helper.Process(context, output);

            // Assert
            Assert.StartsWith("<link rel=\"stylesheet\" data-extra=\"something\" href=\"test.css\"", output.Content);
        }
        
        [Fact]
        public void DoesNotRunWhenARequiredAttributeIsMissing()
        {
            // Arrange
            var context = MakeTagHelperContext(
                attributes: new Dictionary<string, object>
                {
                    // This is commented out on purpose: { "asp-fallback-href", "test.css" },
                    { "asp-fallback-test-class", "hidden" },
                    { "asp-fallback-test-property", "visible" },
                    { "asp-fallback-test-value", "hidden" },
                });
            var output = MakeTagHelperOutput("link");
            var logger = new Mock<ILogger>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(logger.Object);

            // Act
            var helper = new LinkTagHelper
            {
                LoggerFactory = loggerFactory.Object,
                // This is commented out on purpose: FallbackHref = "test.css",
                FallbackTestClass = "hidden",
                FallbackTestProperty = "visible",
                FallbackTestValue = "hidden"
            };
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.ContentSet);
        }
        
        [Fact]
        public void DoesNotRunWhenAllRequiredAttributesAreMissing()
        {
            // Arrange
            var context = MakeTagHelperContext();
            var output = MakeTagHelperOutput("link");
            var logger = new Mock<ILogger>();
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(logger.Object);

            // Act
            var helper = new LinkTagHelper
            {
                LoggerFactory = loggerFactory.Object
            };
            helper.Process(context, output);

            // Assert
            Assert.NotNull(output.TagName);
            Assert.False(output.ContentSet);
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
    }
}