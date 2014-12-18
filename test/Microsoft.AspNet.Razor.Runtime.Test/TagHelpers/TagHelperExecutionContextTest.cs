// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperExecutionContextTest
    {
        [Fact]
        public async Task GetChildContentAsync_CachesValue()
        {
            // Arrange
            var writer = new StringWriter();
            var expectedContent = string.Empty;
            var executionContext = new TagHelperExecutionContext(
                "p",
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    if (string.IsNullOrEmpty(expectedContent))
                    {
                        expectedContent = "Hello from child content: " + Guid.NewGuid().ToString();
                    }

                    writer.Write(expectedContent);

                    return Task.FromResult(result: true);
                },
                startWritingScope: () => { },
                endWritingScope: () => writer);

            // Act
            var content1 = await executionContext.GetChildContentAsync();
            var content2 = await executionContext.GetChildContentAsync();

            // Assert
            Assert.Same(content1, content2);
            Assert.Equal(expectedContent, content1);
            Assert.Equal(expectedContent, content2);
        }

        [Fact]
        public async Task ExecuteChildContentAsync_IsNotMemoized()
        {
            // Arrange
            var childContentExecutionCount = 0;
            var executionContext = new TagHelperExecutionContext(
                "p",
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    childContentExecutionCount++;

                    return Task.FromResult(result: true);
                },
                startWritingScope: () => { },
                endWritingScope: () => new StringWriter());

            // Act
            await executionContext.ExecuteChildContentAsync();
            await executionContext.ExecuteChildContentAsync();
            await executionContext.ExecuteChildContentAsync();

            // Assert
            Assert.Equal(3, childContentExecutionCount);
        }

        public static TheoryData<string, string> DictionaryCaseTestingData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { "class", "CLaSS" },
                    { "Class", "class" },
                    { "Class", "claSS" }
                };
            }
        }

        [MemberData(nameof(DictionaryCaseTestingData))]
        public void HtmlAttributes_IgnoresCase(string originalName, string updatedName)
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p");
            executionContext.HTMLAttributes[originalName] = "hello";

            // Act
            executionContext.HTMLAttributes[updatedName] = "something else";

            // Assert
            var attribute = Assert.Single(executionContext.HTMLAttributes);
            Assert.Equal(new KeyValuePair<string, string>(originalName, "something else"), attribute);
        }

        [MemberData(nameof(DictionaryCaseTestingData))]
        public void AllAttributes_IgnoresCase(string originalName, string updatedName)
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p");
            executionContext.AllAttributes[originalName] = false;

            // Act
            executionContext.AllAttributes[updatedName] = true;

            // Assert
            var attribute = Assert.Single(executionContext.AllAttributes);
            Assert.Equal(new KeyValuePair<string, object>(originalName, true), attribute);
        }

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