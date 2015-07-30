// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperExecutionContextTest
    {

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SelfClosing_ReturnsTrueOrFalseAsExpected(bool selfClosing)
        {
            // Arrange & Act
            var executionContext = new TagHelperExecutionContext("p", selfClosing);

            // Assert
            Assert.Equal(selfClosing, executionContext.SelfClosing);
        }

        [Fact]
        public void ParentItems_SetsItemsProperty()
        {
            // Arrange
            var expectedItems = new Dictionary<object, object>
            {
                { "test-entry", 1234 }
            };

            // Act
            var executionContext = new TagHelperExecutionContext(
                "p",
                selfClosing: false,
                items: expectedItems,
                uniqueId: string.Empty,
                executeChildContentAsync: async () => await Task.FromResult(result: true),
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());

            // Assert
            Assert.NotNull(executionContext.Items);
            Assert.Same(expectedItems, executionContext.Items);
        }

        [Fact]
        public async Task GetChildContentAsync_CachesValue()
        {
            // Arrange
            var defaultTagHelperContent = new DefaultTagHelperContent();
            var expectedContent = string.Empty;
            var executionContext = new TagHelperExecutionContext(
                "p",
                selfClosing: false,
                items: null,
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    if (string.IsNullOrEmpty(expectedContent))
                    {
                        expectedContent = "Hello from child content: " + Guid.NewGuid().ToString();
                    }

                    defaultTagHelperContent.SetContent(expectedContent);

                    return Task.FromResult(result: true);
                },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => defaultTagHelperContent);

            // Act
            var content1 = await executionContext.GetChildContentAsync(useCachedResult: true);
            var content2 = await executionContext.GetChildContentAsync(useCachedResult: true);

            // Assert
            Assert.Equal(expectedContent, content1.GetContent());
            Assert.Equal(expectedContent, content2.GetContent());
        }

        [Fact]
        public async Task GetChildContentAsync_CanExecuteChildrenMoreThanOnce()
        {
            // Arrange
            var executionCount = 0;
            var executionContext = new TagHelperExecutionContext(
                "p",
                selfClosing: false,
                items: null,
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    executionCount++;

                    return Task.FromResult(result: true);
                },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());

            // Act
            await executionContext.GetChildContentAsync(useCachedResult: false);
            await executionContext.GetChildContentAsync(useCachedResult: false);

            // Assert
            Assert.Equal(2, executionCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetChildContentAsync_ReturnsNewObjectEveryTimeItIsCalled(bool useCachedResult)
        {
            // Arrange
            var defaultTagHelperContent = new DefaultTagHelperContent();
            var executionContext = new TagHelperExecutionContext(
                "p",
                selfClosing: false,
                items: null,
                uniqueId: string.Empty,
                executeChildContentAsync: () => { return Task.FromResult(result: true); },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => defaultTagHelperContent);

            // Act
            var content1 = await executionContext.GetChildContentAsync(useCachedResult);
            content1.Append("Hello");
            var content2 = await executionContext.GetChildContentAsync(useCachedResult);
            content2.Append("World!");

            // Assert
            Assert.NotSame(content1, content2);
            Assert.Empty((await executionContext.GetChildContentAsync(useCachedResult)).GetContent());
        }

        [Fact]
        public async Task ExecuteChildContentAsync_IsNotMemoized()
        {
            // Arrange
            var childContentExecutionCount = 0;
            var executionContext = new TagHelperExecutionContext(
                "p",
                selfClosing: false,
                items: null,
                uniqueId: string.Empty,
                executeChildContentAsync: () =>
                {
                    childContentExecutionCount++;

                    return Task.FromResult(result: true);
                },
                startTagHelperWritingScope: () => { },
                endTagHelperWritingScope: () => new DefaultTagHelperContent());

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

        [Theory]
        [MemberData(nameof(DictionaryCaseTestingData))]
        public void HtmlAttributes_IgnoresCase(string originalName, string updatedName)
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
            executionContext.HTMLAttributes[originalName] = "hello";

            // Act
            executionContext.HTMLAttributes[updatedName] = "something else";

            // Assert
            var attribute = Assert.Single(executionContext.HTMLAttributes);
            Assert.Equal(new TagHelperAttribute(originalName, "something else"), attribute);
        }

        [Theory]
        [MemberData(nameof(DictionaryCaseTestingData))]
        public void AllAttributes_IgnoresCase(string originalName, string updatedName)
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
            executionContext.AllAttributes.Add(originalName, value: false);

            // Act
            executionContext.AllAttributes[updatedName].Value = true;

            // Assert
            var attribute = Assert.Single(executionContext.AllAttributes);
            Assert.Equal(new TagHelperAttribute(originalName, true), attribute);
        }

        [Fact]
        public void AddHtmlAttribute_MaintainsHTMLAttributes()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "btn" },
                { "foo", "bar" }
            };

            // Act
            executionContext.AddHtmlAttribute("class", "btn");
            executionContext.AddHtmlAttribute("foo", "bar");

            // Assert
            Assert.Equal(
                expectedAttributes,
                executionContext.HTMLAttributes,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void AddMinimizedHtmlAttribute_MaintainsHTMLAttributes()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("input", selfClosing: true);
            var expectedAttributes = new TagHelperAttributeList
            {
                ["checked"] = new TagHelperAttribute { Name = "checked", Minimized = true },
                ["visible"] = new TagHelperAttribute { Name = "visible", Minimized = true }
            };

            // Act
            executionContext.AddMinimizedHtmlAttribute("checked");
            executionContext.AddMinimizedHtmlAttribute("visible");

            // Assert
            Assert.Equal(
                expectedAttributes,
                executionContext.HTMLAttributes,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void AddMinimizedHtmlAttribute_MaintainsHTMLAttributes_SomeMinimized()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("input", selfClosing: true);
            var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "btn" },
                { "foo", "bar" }
            };
            expectedAttributes.Add(new TagHelperAttribute { Name = "checked", Minimized = true });
            expectedAttributes.Add(new TagHelperAttribute { Name = "visible", Minimized = true });

            // Act
            executionContext.AddHtmlAttribute("class", "btn");
            executionContext.AddHtmlAttribute("foo", "bar");
            executionContext.AddMinimizedHtmlAttribute("checked");
            executionContext.AddMinimizedHtmlAttribute("visible");

            // Assert
            Assert.Equal(
                expectedAttributes,
                executionContext.HTMLAttributes,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void TagHelperExecutionContext_MaintainsAllAttributes()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
            var expectedAttributes = new TagHelperAttributeList
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
            Assert.Equal(
                expectedAttributes,
                executionContext.AllAttributes,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void Add_MaintainsTagHelpers()
        {
            // Arrange
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
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
            var executionContext = new TagHelperExecutionContext("p", selfClosing: false);
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