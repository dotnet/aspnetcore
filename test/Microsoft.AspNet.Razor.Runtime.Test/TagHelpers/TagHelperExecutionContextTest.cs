// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperExecutionContextTest
    {
        [Fact]
        public void AddHtmlAttribute_MaintainsHTMLAttributes()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p");
            var expectedAttributes = new Dictionary<string, string>
            {
                { "class", "btn" },
                { "foo", "bar" }
            };

            // Act
            executionContext.AddHtmlAttribute("class", "btn");
            executionContext.AddHtmlAttribute("foo", "bar");

            // Assert
            Assert.Equal(expectedAttributes, executionContext.HTMLAttributes);
        }

        [Fact]
        public void TagHelperExecutionContext_MaintainsAllAttributes()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p");
            var expectedAttributes = new Dictionary<string, object>
            {
                { "class", "btn" },
                { "something", true },
                { "foo", "bar" }
            };

            // Act
            executionContext.AddHtmlAttribute("class", "btn");
            executionContext.AddTagHelperAttribute("something", true);
            executionContext.AddHtmlAttribute("foo", "bar");

            // Assert
            Assert.Equal(expectedAttributes, executionContext.AllAttributes);
        }

        [Fact]
        public void Add_MaintainsTagHelpers()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p");
            var tagHelper = new PTagHelper();

            // Act
            executionContext.Add(tagHelper);

            // Assert
            var singleTagHelper = Assert.Single(executionContext.TagHelpers);
            Assert.Same(tagHelper, singleTagHelper);
        }

        [Fact]
        public void Add_MaintainsMultipleTagHelpers()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p");
            var tagHelper1 = new PTagHelper();
            var tagHelper2 = new PTagHelper();

            // Act
            executionContext.Add(tagHelper1);
            executionContext.Add(tagHelper2);

            // Assert
            var tagHelpers = executionContext.TagHelpers.ToArray();
            Assert.Equal(2, tagHelpers.Length);
            Assert.Same(tagHelper1, tagHelpers[0]);
            Assert.Same(tagHelper2, tagHelpers[1]);

        }

        private class PTagHelper : TagHelper
        {
        }
    }
}