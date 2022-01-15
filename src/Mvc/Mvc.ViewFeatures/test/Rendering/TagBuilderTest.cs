// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Mvc.Core.Rendering;

public class TagBuilderTest
{
    public static TheoryData<TagRenderMode, string> RenderingTestingData
    {
        get
        {
            return new TheoryData<TagRenderMode, string>
                {
                    { TagRenderMode.StartTag, "<p>" },
                    { TagRenderMode.SelfClosing, "<p />" },
                    { TagRenderMode.Normal, "<p></p>" }
                };
        }
    }

    [Theory]
    [InlineData(false, "Hello", "World")]
    [InlineData(true, "hello", "something else")]
    public void MergeAttribute_IgnoresCase(bool replaceExisting, string expectedKey, string expectedValue)
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.Attributes.Add("Hello", "World");

        // Act
        tagBuilder.MergeAttribute("hello", "something else", replaceExisting);

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes);
        Assert.Equal(new KeyValuePair<string, string>(expectedKey, expectedValue), attribute);
    }

    [Fact]
    public void AddCssClass_IgnoresCase()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.Attributes.Add("ClaSs", "btn");

        // Act
        tagBuilder.AddCssClass("btn-success");

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes);
        Assert.Equal(new KeyValuePair<string, string>("class", "btn btn-success"), attribute);
    }

    [Fact]
    public void GenerateId_IgnoresCase()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.Attributes.Add("ID", "something");

        // Act
        tagBuilder.GenerateId("else", invalidCharReplacement: "-");

        // Assert
        var attribute = Assert.Single(tagBuilder.Attributes);
        Assert.Equal(new KeyValuePair<string, string>("ID", "something"), attribute);
    }

    [Theory]
    [MemberData(nameof(RenderingTestingData))]
    public void WriteTo_IgnoresIdAttributeCase(TagRenderMode renderingMode, string expectedOutput)
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        // An empty value id attribute should not be rendered via ToString.
        tagBuilder.Attributes.Add("ID", string.Empty);
        tagBuilder.TagRenderMode = renderingMode;

        // Act
        using (var writer = new StringWriter())
        {
            tagBuilder.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal(expectedOutput, writer.ToString());
        }
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("a", "a")]
    [InlineData("0", "z")]
    [InlineData("-", "z")]
    [InlineData(",", "z")]
    [InlineData("00Hello,World", "z0Hello-World")]
    [InlineData(",,Hello,,World,,", "z-Hello--World--")]
    [InlineData("-_:Hello-_:Hello-_:", "z_:Hello-_:Hello-_:")]
    [InlineData("HelloWorld", "HelloWorld")]
    [InlineData("�HelloWorld", "zHelloWorld")]
    [InlineData("Hello�World", "Hello-World")]
    public void CreateSanitizedIdCreatesId(string input, string output)
    {
        // Arrange
        var result = TagBuilder.CreateSanitizedId(input, "-");

        // Assert
        Assert.Equal(output, result);
    }

    [Theory]
    [InlineData("attribute", "value", "<p attribute=\"HtmlEncode[[value]]\"></p>")]
    [InlineData("attribute", null, "<p attribute=\"\"></p>")]
    [InlineData("attribute", "", "<p attribute=\"\"></p>")]
    public void WriteTo_WriteEmptyAttribute_WhenValueIsNullOrEmpty(
        string attributeKey,
        string attributeValue,
        string expectedOutput)
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");

        // Act
        tagBuilder.Attributes.Add(attributeKey, attributeValue);

        // Assert
        using (var writer = new StringWriter())
        {
            tagBuilder.WriteTo(writer, new HtmlTestEncoder());
            Assert.Equal(expectedOutput, writer.ToString());
        }
    }

    [Fact]
    public void WriteTo_IncludesInnerHtml()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.InnerHtml.AppendHtml("<span>Hello</span>");
        tagBuilder.InnerHtml.Append(", World!");

        // Act
        using (var writer = new StringWriter())
        {
            tagBuilder.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("<p><span>Hello</span>HtmlEncode[[, World!]]</p>", writer.ToString());
        }

        Assert.True(tagBuilder.HasInnerHtml);
    }

    [Fact]
    public void ReadingInnerHtml_LeavesHasInnerHtmlFalse()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");

        // Act
        var innerHtml = tagBuilder.InnerHtml;

        // Assert
        Assert.False(tagBuilder.HasInnerHtml);
        Assert.NotNull(innerHtml);
    }

    [Fact]
    public void RenderStartTag_RendersExpectedStartTag()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");

        // Act
        var tag = tagBuilder.RenderStartTag();

        // Assert
        Assert.Equal("<p>", HtmlContentUtilities.HtmlContentToString(tag));
    }

    [Fact]
    public void RenderStartTag_RendersExpectedStartTag_TagBuilderRendersAsExpected()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.TagRenderMode = TagRenderMode.EndTag;

        // Act
        var tag = tagBuilder.RenderStartTag();

        // Assert
        Assert.Equal("<p>", HtmlContentUtilities.HtmlContentToString(tag));
        Assert.Equal("</p>", HtmlContentUtilities.HtmlContentToString(tagBuilder));
    }

    [Fact]
    public void RenderEndTag_RendersExpectedEndTag()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");

        // Act
        var tag = tagBuilder.RenderEndTag();

        // Assert
        Assert.Equal("</p>", HtmlContentUtilities.HtmlContentToString(tag));
    }

    [Fact]
    public void RenderEndTag_RendersExpectedEndTag_TagBuilderRendersAsExpected()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.TagRenderMode = TagRenderMode.Normal;

        // Act
        var tag = tagBuilder.RenderEndTag();

        // Assert
        Assert.Equal("</p>", HtmlContentUtilities.HtmlContentToString(tag));
        Assert.Equal("<p></p>", HtmlContentUtilities.HtmlContentToString(tagBuilder));
    }

    [Fact]
    public void RenderSelfClosingTag_RendersExpectedSelfClosingTag()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");

        // Act
        var tag = tagBuilder.RenderSelfClosingTag();

        // Assert
        Assert.Equal("<p />", HtmlContentUtilities.HtmlContentToString(tag));
    }

    [Fact]
    public void RenderBody_RendersExpectedBody()
    {
        // Arrange
        var tagBuilder = new TagBuilder("p");
        tagBuilder.InnerHtml.AppendHtml("<span>Hello</span>");

        // Act
        var tag = tagBuilder.RenderBody();

        // Assert
        Assert.Equal("<span>Hello</span>", HtmlContentUtilities.HtmlContentToString(tag));
    }

    [Fact]
    public void Constructor_Copy_CopiesTagRenderMode()
    {
        // Arrange
        var originalTagBuilder = new TagBuilder("p");
        originalTagBuilder.TagRenderMode = TagRenderMode.SelfClosing;

        // Act
        var clonedTagBuilder = new TagBuilder(originalTagBuilder);


        // Assert
        Assert.Equal(originalTagBuilder.TagRenderMode, clonedTagBuilder.TagRenderMode);
    }

    [Fact]
    public void Constructor_Copy_DoesShallowCopyOfInnerHtml()
    {
        // Arrange
        var originalTagBuilder = new TagBuilder("p");
        originalTagBuilder.InnerHtml.AppendHtml("<span>Hello</span>");

        // Act
        var clonedTagBuilder = new TagBuilder(originalTagBuilder);

        // Assert
        Assert.NotEqual(originalTagBuilder.InnerHtml, clonedTagBuilder.InnerHtml);
        Assert.Equal(HtmlContentUtilities.HtmlContentToString(originalTagBuilder.RenderBody()), HtmlContentUtilities.HtmlContentToString(clonedTagBuilder.RenderBody()));
    }

    [Fact]
    public void Constructor_Copy_DoesShallowCopyOfAttributes()
    {
        // Arrange
        var originalTagBuilder = new TagBuilder("p");
        originalTagBuilder.AddCssClass("class1");

        // Act
        var clonedTagBuilder = new TagBuilder(originalTagBuilder);

        // Assert
        Assert.NotSame(originalTagBuilder.Attributes, clonedTagBuilder.Attributes);
        Assert.Equal(originalTagBuilder.Attributes, clonedTagBuilder.Attributes);
    }

    [Fact]
    public void Constructor_Copy_CopiesTagName()
    {
        // Arrange
        var originalTagBuilder = new TagBuilder("p");

        // Act

        var clonedTagBuilder = new TagBuilder(originalTagBuilder);

        // Assert
        Assert.Equal(originalTagBuilder.TagName, clonedTagBuilder.TagName);
    }
}
