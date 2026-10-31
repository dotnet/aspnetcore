// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

public class TagHelperExecutionContextTest
{
    [Fact]
    public async Task SetOutputContentAsync_CanHandleExceptionThrowingChildContent()
    {
        // Arrange
        var calledEnd = false;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () => throw new Exception(),
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () =>
            {
                calledEnd = true;
                return new DefaultTagHelperContent();
            });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await executionContext.SetOutputContentAsync());
        Assert.True(calledEnd);
    }

    [Fact]
    public async Task GetChildContentAsync_CanHandleExceptionThrowingChildContent()
    {
        // Arrange
        var calledEnd = false;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () => throw new Exception(),
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () =>
            {
                calledEnd = true;
                return new DefaultTagHelperContent();
            });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            async () => await executionContext.GetChildContentAsync(useCachedResult: false, encoder: null));
        Assert.True(calledEnd);
    }

    [Fact]
    public async Task SetOutputContentAsync_SetsOutputsContent()
    {
        // Arrange
        var tagHelperContent = new DefaultTagHelperContent();
        var content = "Hello from child content";
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () =>
            {
                tagHelperContent.SetContent(content);

                return Task.FromResult(result: true);
            },
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => tagHelperContent);

        // Act
        await executionContext.SetOutputContentAsync();

        // Assert
        Assert.Equal(content, executionContext.Output.Content.GetContent());
    }

    [Fact]
    public async Task ExecutionContext_Reinitialize_UpdatesTagHelperOutputAsExpected()
    {
        // Arrange
        var tagName = "div";
        var tagMode = TagMode.StartTagOnly;
        var callCount = 0;
        Func<Task> executeChildContentAsync = () =>
        {
            callCount++;
            return Task.FromResult(true);
        };
        Action<HtmlEncoder> startTagHelperWritingScope = _ => { };
        Func<TagHelperContent> endTagHelperWritingScope = () => null;
        var executionContext = new TagHelperExecutionContext(
            tagName,
            tagMode,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: executeChildContentAsync,
            startTagHelperWritingScope: startTagHelperWritingScope,
            endTagHelperWritingScope: endTagHelperWritingScope);
        var updatedTagName = "p";
        var updatedTagMode = TagMode.SelfClosing;
        var updatedCallCount = 0;
        Func<Task> updatedExecuteChildContentAsync = () =>
        {
            updatedCallCount++;
            return Task.FromResult(true);
        };
        executionContext.AddHtmlAttribute(new TagHelperAttribute("something"));

        // Act - 1
        executionContext.Reinitialize(
            updatedTagName,
            updatedTagMode,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: updatedExecuteChildContentAsync);
        executionContext.AddHtmlAttribute(new TagHelperAttribute("Another attribute"));

        // Assert - 1
        var output = executionContext.Output;
        Assert.Equal(updatedTagName, output.TagName);
        Assert.Equal(updatedTagMode, output.TagMode);
        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("Another attribute", attribute.Name);

        // Act - 2
        await output.GetChildContentAsync();

        // Assert - 2
        Assert.Equal(0, callCount);
        Assert.Equal(1, updatedCallCount);
    }

    [Fact]
    public void ExecutionContext_Reinitialize_UpdatesTagHelperContextAsExpected()
    {
        // Arrange
        var tagName = "div";
        var tagMode = TagMode.StartTagOnly;
        var items = new Dictionary<object, object>();
        var uniqueId = "some unique id";
        var callCount = 0;
        Func<Task> executeChildContentAsync = () =>
        {
            callCount++;
            return Task.FromResult(true);
        };
        Action<HtmlEncoder> startWritingScope = _ => { };
        Func<TagHelperContent> endWritingScope = () => null;
        var executionContext = new TagHelperExecutionContext(
            tagName,
            tagMode,
            items,
            uniqueId,
            executeChildContentAsync,
            startWritingScope,
            endWritingScope);
        var updatedItems = new Dictionary<object, object>();
        var updatedUniqueId = "another unique id";
        executionContext.AddHtmlAttribute(new TagHelperAttribute("something"));

        // Act
        executionContext.Reinitialize(
            tagName,
            tagMode,
            updatedItems,
            updatedUniqueId,
            executeChildContentAsync);
        executionContext.AddHtmlAttribute(new TagHelperAttribute("Another attribute"));

        // Assert
        var context = executionContext.Context;
        var attribute = Assert.Single(context.AllAttributes);
        Assert.Equal(tagName, context.TagName);
        Assert.Equal("Another attribute", attribute.Name);
        Assert.Equal(updatedUniqueId, context.UniqueId);
        Assert.Same(updatedItems, context.Items);
    }

    [Theory]
    [InlineData(TagMode.SelfClosing)]
    [InlineData(TagMode.StartTagAndEndTag)]
    [InlineData(TagMode.StartTagOnly)]
    public void ExecutionContext_CreateTagHelperOutput_ReturnsExpectedTagMode(TagMode tagMode)
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext("p", tagMode);

        // Act
        var output = executionContext.Output;

        // Assert
        Assert.Equal(tagMode, output.TagMode);
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
            tagMode: TagMode.StartTagAndEndTag,
            items: expectedItems,
            uniqueId: string.Empty,
            executeChildContentAsync: async () => await Task.FromResult(result: true),
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Assert
        Assert.NotNull(executionContext.Items);
        Assert.Same(expectedItems, executionContext.Items);
    }

    public static TheoryData<HtmlEncoder> HtmlEncoderData
    {
        get
        {
            return new TheoryData<HtmlEncoder>
                {
                    null,
                    HtmlEncoder.Default,
                    NullHtmlEncoder.Default,
                    new HtmlTestEncoder(),
                };
        }
    }

    public static TheoryData<HtmlEncoder> HtmlEncoderDataLessNull
    {
        get
        {
            var data = new TheoryData<HtmlEncoder>();
            foreach (var encoder in HtmlEncoderData)
            {
                if (encoder != null)
                {
                    data.Add(encoder);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(HtmlEncoderData))]
    public async Task GetChildContentAsync_ReturnsExpectedContent(HtmlEncoder encoder)
    {
        // Arrange
        var tagHelperContent = new DefaultTagHelperContent();
        var executionCount = 0;
        var content = "Hello from child content";
        var expectedContent = $"HtmlEncode[[{content}]]";
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () =>
            {
                executionCount++;
                tagHelperContent.SetContent(content);

                return Task.FromResult(result: true);
            },
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => tagHelperContent);

        // Act
        var actualContent = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: encoder);

        // Assert
        Assert.Equal(expectedContent, actualContent.GetContent(new HtmlTestEncoder()));
    }

    [Theory]
    [MemberData(nameof(HtmlEncoderData))]
    public async Task GetChildContentAsync_StartsWritingScopeWithGivenEncoder(HtmlEncoder encoder)
    {
        // Arrange
        HtmlEncoder passedEncoder = null;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () => Task.FromResult(result: true),
            startTagHelperWritingScope: encoderArgument => passedEncoder = encoderArgument,
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        await executionContext.GetChildContentAsync(useCachedResult: true, encoder: encoder);

        // Assert
        Assert.Same(encoder, passedEncoder);
    }

    [Theory]
    [MemberData(nameof(HtmlEncoderData))]
    public async Task GetChildContentAsync_CachesValue(HtmlEncoder encoder)
    {
        // Arrange
        var executionCount = 0;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () =>
            {
                executionCount++;
                return Task.FromResult(result: true);
            },
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        var content1 = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: encoder);
        var content2 = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: encoder);

        // Assert
        Assert.Equal(1, executionCount);
    }

    [Theory]
    [MemberData(nameof(HtmlEncoderDataLessNull))]
    public async Task GetChildContentAsync_CachesValuePerEncoder(HtmlEncoder encoder)
    {
        // Arrange
        var executionCount = 0;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () =>
            {
                executionCount++;
                return Task.FromResult(result: true);
            },
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        var content1 = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: null);
        var content2 = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: encoder);

        // Assert
        Assert.Equal(2, executionCount);
    }

    [Theory]
    [MemberData(nameof(HtmlEncoderData))]
    public async Task GetChildContentAsync_CachesValuePerEncoderInstance(HtmlEncoder encoder)
    {
        // Arrange
        var executionCount = 0;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () =>
            {
                executionCount++;
                return Task.FromResult(result: true);
            },
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // HtmlEncoderData includes another HtmlTestEncoder instance but method compares HtmlEncoder instances.
        var firstEncoder = new HtmlTestEncoder();

        // Act
        var content1 = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: firstEncoder);
        var content2 = await executionContext.GetChildContentAsync(useCachedResult: true, encoder: encoder);

        // Assert
        Assert.Equal(2, executionCount);
    }

    [Theory]
    [MemberData(nameof(HtmlEncoderData))]
    public async Task GetChildContentAsync_CanExecuteChildrenMoreThanOnce(HtmlEncoder encoder)
    {
        // Arrange
        var executionCount = 0;
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () =>
            {
                executionCount++;
                return Task.FromResult(result: true);
            },
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        await executionContext.GetChildContentAsync(useCachedResult: false, encoder: encoder);
        await executionContext.GetChildContentAsync(useCachedResult: false, encoder: encoder);

        // Assert
        Assert.Equal(2, executionCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetChildContentAsync_ReturnsNewObjectEveryTimeItIsCalled(bool useCachedResult)
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext(
            "p",
            tagMode: TagMode.StartTagAndEndTag,
            items: new Dictionary<object, object>(),
            uniqueId: string.Empty,
            executeChildContentAsync: () => Task.FromResult(result: true),
            startTagHelperWritingScope: _ => { },
            endTagHelperWritingScope: () => new DefaultTagHelperContent());

        // Act
        var content1 = await executionContext.GetChildContentAsync(useCachedResult, encoder: null);
        var content2 = await executionContext.GetChildContentAsync(useCachedResult, encoder: null);

        // Assert
        Assert.NotSame(content1, content2);
    }

    [Fact]
    public void AddHtmlAttribute_MaintainsHtmlAttributes()
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "btn" },
            };
        expectedAttributes.Add(new TagHelperAttribute("type", "text", HtmlAttributeValueStyle.SingleQuotes));

        // Act
        executionContext.AddHtmlAttribute("class", "btn", HtmlAttributeValueStyle.DoubleQuotes);
        executionContext.AddHtmlAttribute("type", "text", HtmlAttributeValueStyle.SingleQuotes);
        var output = executionContext.Output;

        // Assert
        Assert.Equal(
            expectedAttributes,
            output.Attributes,
            CaseSensitiveTagHelperAttributeComparer.Default);
    }

    [Fact]
    public void AddHtmlAttribute_MaintainsMinimizedHtmlAttributes()
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext("input", tagMode: TagMode.StartTagOnly);
        var expectedAttributes = new TagHelperAttributeList
            {
                new TagHelperAttribute("checked"),
                new TagHelperAttribute("visible"),
            };

        // Act
        executionContext.AddHtmlAttribute(new TagHelperAttribute("checked"));
        executionContext.AddHtmlAttribute(new TagHelperAttribute("visible"));
        var output = executionContext.Output;

        // Assert
        Assert.Equal(
            expectedAttributes,
            output.Attributes,
            CaseSensitiveTagHelperAttributeComparer.Default);
    }

    [Fact]
    public void AddHtmlAttribute_MaintainsHtmlAttributes_VariousStyles()
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext("input", tagMode: TagMode.SelfClosing);
        var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "btn" },
                { "foo", "bar" }
            };
        expectedAttributes.Add(new TagHelperAttribute("valid", "true", HtmlAttributeValueStyle.NoQuotes));
        expectedAttributes.Add(new TagHelperAttribute("type", "text", HtmlAttributeValueStyle.SingleQuotes));
        expectedAttributes.Add(new TagHelperAttribute(name: "checked"));
        expectedAttributes.Add(new TagHelperAttribute(name: "visible"));

        // Act
        executionContext.AddHtmlAttribute("class", "btn", HtmlAttributeValueStyle.DoubleQuotes);
        executionContext.AddHtmlAttribute("foo", "bar", HtmlAttributeValueStyle.DoubleQuotes);
        executionContext.AddHtmlAttribute("valid", "true", HtmlAttributeValueStyle.NoQuotes);
        executionContext.AddHtmlAttribute("type", "text", HtmlAttributeValueStyle.SingleQuotes);
        executionContext.AddHtmlAttribute(new TagHelperAttribute("checked"));
        executionContext.AddHtmlAttribute(new TagHelperAttribute("visible"));
        var output = executionContext.Output;

        // Assert
        Assert.Equal(
            expectedAttributes,
            output.Attributes,
            CaseSensitiveTagHelperAttributeComparer.Default);
    }

    [Fact]
    public void TagHelperExecutionContext_MaintainsAllAttributes()
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
        var expectedAttributes = new TagHelperAttributeList
            {
                { "class", "btn" },
            };
        expectedAttributes.Add(new TagHelperAttribute("something", true, HtmlAttributeValueStyle.SingleQuotes));
        expectedAttributes.Add(new TagHelperAttribute("type", "text", HtmlAttributeValueStyle.NoQuotes));

        // Act
        executionContext.AddHtmlAttribute("class", "btn", HtmlAttributeValueStyle.DoubleQuotes);
        executionContext.AddTagHelperAttribute("something", true, HtmlAttributeValueStyle.SingleQuotes);
        executionContext.AddHtmlAttribute("type", "text", HtmlAttributeValueStyle.NoQuotes);
        var context = executionContext.Context;

        // Assert
        Assert.Equal(
            expectedAttributes,
            context.AllAttributes,
            CaseSensitiveTagHelperAttributeComparer.Default);
    }

    [Fact]
    public void Add_MaintainsTagHelpers()
    {
        // Arrange
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
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
        var executionContext = new TagHelperExecutionContext("p", TagMode.StartTagAndEndTag);
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
