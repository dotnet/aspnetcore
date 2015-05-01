// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperOutputTest
    {
        [Fact]
        public void PreElement_SetContent_ChangesValue()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PreElement.SetContent("Hello World");

            // Act & Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.NotNull(tagHelperOutput.Content);
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Equal("Hello World", tagHelperOutput.PreElement.GetContent());
        }

        [Fact]
        public void PostElement_SetContent_ChangesValue()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PostElement.SetContent("Hello World");

            // Act & Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.NotNull(tagHelperOutput.Content);
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Equal("Hello World", tagHelperOutput.PostElement.GetContent());
        }

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
        public void PreContent_SetContent_ChangesValue()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PreContent.SetContent("Hello World");

            // Act & Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.NotNull(tagHelperOutput.Content);
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Equal("Hello World", tagHelperOutput.PreContent.GetContent());
        }

        [Fact]
        public void Content_SetContent_ChangesValue()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.Content.SetContent("Hello World");

            // Act & Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.NotNull(tagHelperOutput.Content);
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Equal("Hello World", tagHelperOutput.Content.GetContent());
        }

        [Fact]
        public void PostContent_SetContent_ChangesValue()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");
            tagHelperOutput.PostContent.SetContent("Hello World");

            // Act & Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.NotNull(tagHelperOutput.Content);
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Equal("Hello World", tagHelperOutput.PostContent.GetContent());
        }

        [Fact]
        public void SuppressOutput_Sets_AllContent_ToNullOrEmpty()
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
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.Empty(tagHelperOutput.PreElement.GetContent());
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.Empty(tagHelperOutput.PreContent.GetContent());
            Assert.NotNull(tagHelperOutput.Content);
            Assert.Empty(tagHelperOutput.Content.GetContent());
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.Empty(tagHelperOutput.PostContent.GetContent());
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Empty(tagHelperOutput.PostElement.GetContent());
        }

        [Fact]
        public void SuppressOutput_PreventsTagOutput()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p",
                new TagHelperAttributeList
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                });
            tagHelperOutput.PreContent.Append("Pre Content");
            tagHelperOutput.Content.Append("Content");
            tagHelperOutput.PostContent.Append("Post Content");

            // Act
            tagHelperOutput.SuppressOutput();

            // Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.Empty(tagHelperOutput.PreElement.GetContent());
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.Empty(tagHelperOutput.PreContent.GetContent());
            Assert.NotNull(tagHelperOutput.Content);
            Assert.Empty(tagHelperOutput.Content.GetContent());
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.Empty(tagHelperOutput.PostContent.GetContent());
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Empty(tagHelperOutput.PostElement.GetContent());
        }

        [Theory]
        [InlineData("class", "ClASs")]
        [InlineData("CLaSs", "class")]
        [InlineData("cLaSs", "cLasS")]
        public void Attributes_IgnoresCase(string originalName, string updateName)
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p",
                new TagHelperAttributeList
                {
                    { originalName, "btn" },
                });

            // Act
            tagHelperOutput.Attributes[updateName] = "super button";

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(
                new TagHelperAttribute(updateName, "super button"),
                attribute,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }
    }
}