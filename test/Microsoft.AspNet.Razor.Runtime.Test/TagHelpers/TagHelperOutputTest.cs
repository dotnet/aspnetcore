// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Runtime.TagHelpers.Test;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperOutputTest
    {
        [Fact]
        public void TagName_CanSetToNullInCtor()
        {
            // Arrange & Act
            var tagHelperOutput = new TagHelperOutput(null);

            // Assert
            Assert.Null(tagHelperOutput.TagName);
        }

        [Fact]
        public void TagName_CanSetToNull()
        {
            // Arrange & Act
            var tagHelperOutput = new TagHelperOutput("p")
            {
                TagName = null
            };

            // Assert
            Assert.Null(tagHelperOutput.TagName);
        }

        [Fact]
        public void GenerateStartTag_ReturnsFullStartTag()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p", attributes:
                new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                },
                htmlEncoder: new NullHtmlEncoder());

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p class=\"btn\" something=\"   spaced    \">", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsNoAttributeStartTag()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p>", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsSelfClosingStartTag_Attributes()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p",
                attributes: new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                },
                htmlEncoder: new NullHtmlEncoder());

            tagHelperOutput.SelfClosing = true;

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p class=\"btn\" something=\"   spaced    \" />", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsSelfClosingStartTag_NoAttributes()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            tagHelperOutput.SelfClosing = true;

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p />", output);
        }

        [Fact]
        public void GenerateStartTag_UsesProvidedHtmlEncoder()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p",
                attributes: new Dictionary<string, string>
                {
                    { "hello", "world" },
                },
                htmlEncoder: new PseudoHtmlEncoder());

            tagHelperOutput.SelfClosing = true;

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p hello=\"HtmlEncode[[world]]\" />", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsNothingIfWhitespaceTagName()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("  ",
                attributes: new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                },
                htmlEncoder: new NullHtmlEncoder())
            {
                SelfClosing = true
            };

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Empty(output);
        }


        [Fact]
        public void GenerateEndTag_ReturnsNothingIfWhitespaceTagName()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(" ");
            tagHelperOutput.Content.Append("Hello World");

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Empty(output);
        }

        [Fact]
        public void GeneratePreContent_ReturnsPreContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PreContent.Append("Hello World");

            // Act
            var output = tagHelperOutput.GeneratePreContent();

            // Assert
            var result = Assert.IsType<DefaultTagHelperContent>(output);
            Assert.Equal("Hello World", result.GetContent());
        }

        [Fact]
        public void GeneratePreContent_ReturnsNothingIfSelfClosingWhenTagNameIsNotNullOrWhitespace()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true
            };
            tagHelperOutput.PreContent.Append("Hello World");

            // Act
            var output = tagHelperOutput.GeneratePreContent();

            // Assert
            Assert.Null(output);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("\t", true )]
        [InlineData(null, false)]
        [InlineData("\t", false)]
        public void GeneratePreContent_ReturnsPreContentIfTagNameIsNullOrWhitespace(string tagName, bool selfClosing)
        {
            // Arrange
            var expectedContent = "Hello World";

            var tagHelperOutput = new TagHelperOutput(tagName)
            {
                SelfClosing = selfClosing
            };
            tagHelperOutput.PreContent.Append(expectedContent);

            // Act
            var output = tagHelperOutput.GeneratePreContent();

            // Assert
            var result = Assert.IsType<DefaultTagHelperContent>(output);
            Assert.Equal(expectedContent, result.GetContent());
        }

        [Fact]
        public void GenerateContent_ReturnsContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.Content.Append("Hello World");

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            var result = Assert.IsType<DefaultTagHelperContent>(output);
            Assert.Equal("Hello World", result.GetContent());
        }


        [Fact]
        public void GenerateContent_ReturnsNothingIfSelfClosingWhenTagNameIsNotNullOrWhitespace()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true
            };
            tagHelperOutput.Content.Append("Hello World");

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            Assert.Null(output);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("\t", true )]
        [InlineData(null, false)]
        [InlineData("\t", false)]
        public void GenerateContent_ReturnsContentIfTagNameIsNullOrWhitespace(string tagName, bool selfClosing)
        {
            // Arrange
            var expectedContent = "Hello World";

            var tagHelperOutput = new TagHelperOutput(tagName)
            {
                SelfClosing = selfClosing
            };
            tagHelperOutput.Content.Append(expectedContent);

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            var result = Assert.IsType<DefaultTagHelperContent>(output);
            Assert.Equal(expectedContent, result.GetContent());
        }

        [Fact]
        public void GeneratePostContent_ReturnsPostContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PostContent.Append("Hello World");

            // Act
            var output = tagHelperOutput.GeneratePostContent();

            // Assert
            var result = Assert.IsType<DefaultTagHelperContent>(output);
            Assert.Equal("Hello World", result.GetContent());
        }

        [Fact]
        public void GeneratePostContent_ReturnsNothingIfSelfClosingWhenTagNameIsNotNullOrWhitespace()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true
            };
            tagHelperOutput.PostContent.Append("Hello World");

            // Act
            var output = tagHelperOutput.GeneratePostContent();

            // Assert
            Assert.Null(output);
        }

        [Fact]
        public void GenerateEndTag_ReturnsEndTag()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Equal("</p>", output);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("\t", true )]
        [InlineData(null, false)]
        [InlineData("\t", false)]
        public void GeneratePostContent_ReturnsPostContentIfTagNameIsNullOrWhitespace(string tagName, bool selfClosing)
        {
            // Arrange
            var expectedContent = "Hello World";

            var tagHelperOutput = new TagHelperOutput(tagName)
            {
                SelfClosing = selfClosing
            };
            tagHelperOutput.PostContent.Append(expectedContent);

            // Act
            var output = tagHelperOutput.GeneratePostContent();

            // Assert
            var result = Assert.IsType<DefaultTagHelperContent>(output);
            Assert.Equal(expectedContent, result.GetContent());
        }

        [Fact]
        public void GenerateEndTag_ReturnsNothingIfSelfClosing()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true
            };

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Empty(output);
        }

        [Fact]
        public void SuppressOutput_Sets_TagName_Content_PreContent_PostContent_ToNull()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PreContent.Append("Pre Content");
            tagHelperOutput.Content.Append("Content");
            tagHelperOutput.PostContent.Append("Post Content");

            // Act
            tagHelperOutput.SuppressOutput();

            // Assert
            Assert.Null(tagHelperOutput.TagName);
            var result = Assert.IsType<DefaultTagHelperContent>(tagHelperOutput.PreContent);
            Assert.Empty(result.GetContent());
            result = Assert.IsType<DefaultTagHelperContent>(tagHelperOutput.Content);
            Assert.Empty(result.GetContent());
            result = Assert.IsType<DefaultTagHelperContent>(tagHelperOutput.PostContent);
            Assert.Empty(result.GetContent());
        }

        [Fact]
        public void SuppressOutput_PreventsTagOutput()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p",
                attributes: new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                },
                htmlEncoder: new NullHtmlEncoder());
            tagHelperOutput.PreContent.Append("Pre Content");
            tagHelperOutput.Content.Append("Content");
            tagHelperOutput.PostContent.Append("Post Content");

            // Act
            tagHelperOutput.SuppressOutput();

            // Assert
            Assert.Empty(tagHelperOutput.GenerateStartTag());
            var result = Assert.IsType<DefaultTagHelperContent>(tagHelperOutput.GeneratePreContent());
            Assert.Empty(result.GetContent());
            result = Assert.IsType<DefaultTagHelperContent>(tagHelperOutput.GenerateContent());
            Assert.Empty(result.GetContent());
            result = Assert.IsType<DefaultTagHelperContent>(tagHelperOutput.GeneratePostContent());
            Assert.Empty(result.GetContent());
            Assert.Empty(tagHelperOutput.GenerateEndTag());
        }

        [Theory]
        [InlineData("class", "ClASs")]
        [InlineData("CLaSs", "class")]
        [InlineData("cLaSs", "cLasS")]
        public void Attributes_IgnoresCase(string originalName, string updateName)
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p",
                attributes: new Dictionary<string, string>
                {
                    { originalName, "btn" },
                },
                htmlEncoder: new NullHtmlEncoder());

            // Act
            tagHelperOutput.Attributes[updateName] = "super button";

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(new KeyValuePair<string, string>(originalName, "super button"), attribute);
        }
    }
}