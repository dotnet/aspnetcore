// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
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
        public void Content_CanSetToNull()
        {
            // Arrange & Act
            var tagHelperOutput = new TagHelperOutput("p")
            {
                Content = null
            };

            // Assert
            Assert.Null(tagHelperOutput.Content);
        }

        [Fact]
        public void PreContent_CanSetToNull()
        {
            // Arrange & Act
            var tagHelperOutput = new TagHelperOutput("p")
            {
                PreContent = null
            };

            // Assert
            Assert.Null(tagHelperOutput.PreContent);
        }

        [Fact]
        public void PostContent_CanSetToNull()
        {
            // Arrange & Act
            var tagHelperOutput = new TagHelperOutput("p")
            {
                PostContent = null
            };

            // Assert
            Assert.Null(tagHelperOutput.PostContent);
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
                });

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
                });

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
        public void GenerateStartTag_ReturnsNothingIfWhitespaceTagName()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("  ",
                attributes: new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                })
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
            var tagHelperOutput = new TagHelperOutput(" ")
            {
                Content = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Empty(output);
        }

        [Fact]
        public void GeneratePreContent_ReturnsPreContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                PreContent = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GeneratePreContent();

            // Assert
            Assert.Equal("Hello World", output);
        }

        [Fact]
        public void GeneratePreContent_ReturnsNothingIfSelfClosingWhenTagNameIsNotNullOrWhitespace()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true,
                PreContent = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GeneratePreContent();

            // Assert
            Assert.Empty(output);
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
                SelfClosing = selfClosing,
                PreContent = expectedContent
            };

            // Act
            var output = tagHelperOutput.GeneratePreContent();

            // Assert
            Assert.Same(expectedContent, output);
        }

        [Fact]
        public void GenerateContent_ReturnsContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                Content = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            Assert.Equal("Hello World", output);
        }


        [Fact]
        public void GenerateContent_ReturnsNothingIfSelfClosingWhenTagNameIsNotNullOrWhitespace()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true,
                Content = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            Assert.Empty(output);
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
                SelfClosing = selfClosing,
                Content = expectedContent
            };

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            Assert.Same(expectedContent, output);
        }

        [Fact]
        public void GeneratePostContent_ReturnsPostContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                PostContent = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GeneratePostContent();

            // Assert
            Assert.Equal("Hello World", output);
        }

        [Fact]
        public void GeneratePostContent_ReturnsNothingIfSelfClosingWhenTagNameIsNotNullOrWhitespace()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true,
                PostContent = "Hello World"
            };

            // Act
            var output = tagHelperOutput.GeneratePostContent();

            // Assert
            Assert.Empty(output);
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
                SelfClosing = selfClosing,
                PostContent = expectedContent
            };

            // Act
            var output = tagHelperOutput.GeneratePostContent();

            // Assert
            Assert.Equal(expectedContent, output);
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
            var tagHelperOutput = new TagHelperOutput("p")
            {
                PreContent = "Pre Content",
                Content = "Content",
                PostContent = "Post Content"
            };

            // Act
            tagHelperOutput.SuppressOutput();

            // Assert
            Assert.Null(tagHelperOutput.TagName);
            Assert.Null(tagHelperOutput.PreContent);
            Assert.Null(tagHelperOutput.Content);
            Assert.Null(tagHelperOutput.PostContent);
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
                })
            {
                PreContent = "Pre Content",
                Content = "Content",
                PostContent = "Post Content"
            };

            // Act
            tagHelperOutput.SuppressOutput();

            // Assert
            Assert.Empty(tagHelperOutput.GenerateStartTag());
            Assert.Null(tagHelperOutput.GeneratePreContent());
            Assert.Null(tagHelperOutput.GenerateContent());
            Assert.Null(tagHelperOutput.GeneratePostContent());
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
                });

            // Act
            tagHelperOutput.Attributes[updateName] = "super button";

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(new KeyValuePair<string, string>(originalName, "super button"), attribute);
        }
    }
}