// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperOutputTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetChildContentAsync_PassesUseCachedResultAsExpected(bool expectedUseCachedResultValue)
        {
            // Arrange
            bool? useCachedResultValue = null;
            var output = new TagHelperOutput(
                tagName: "p",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: useCachedResult =>
                {
                    useCachedResultValue = useCachedResult;
                    return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
                });

            // Act
            await output.GetChildContentAsync(expectedUseCachedResultValue);

            // Assert
            Assert.Equal(expectedUseCachedResultValue, useCachedResultValue);
        }

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
            Assert.Equal(
                "HtmlEncode[[Hello World]]",
                tagHelperOutput.PreElement.GetContent(new HtmlTestEncoder()));
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
            Assert.Equal(
                "HtmlEncode[[Hello World]]",
                tagHelperOutput.PostElement.GetContent(new HtmlTestEncoder()));
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
            Assert.Equal(
                "HtmlEncode[[Hello World]]",
                tagHelperOutput.PreContent.GetContent(new HtmlTestEncoder()));
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
            Assert.Equal(
                "HtmlEncode[[Hello World]]",
                tagHelperOutput.Content.GetContent(new HtmlTestEncoder()));
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
            Assert.Equal(
                "HtmlEncode[[Hello World]]",
                tagHelperOutput.PostContent.GetContent(new HtmlTestEncoder()));
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
            Assert.Empty(tagHelperOutput.PreElement.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.Empty(tagHelperOutput.PreContent.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.Content);
            Assert.Empty(tagHelperOutput.Content.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.Empty(tagHelperOutput.PostContent.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Empty(tagHelperOutput.PostElement.GetContent(new HtmlTestEncoder()));
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
                },
                (cachedResult) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelperOutput.PreContent.Append("Pre Content");
            tagHelperOutput.Content.Append("Content");
            tagHelperOutput.PostContent.Append("Post Content");

            // Act
            tagHelperOutput.SuppressOutput();

            // Assert
            Assert.NotNull(tagHelperOutput.PreElement);
            Assert.Empty(tagHelperOutput.PreElement.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.PreContent);
            Assert.Empty(tagHelperOutput.PreContent.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.Content);
            Assert.Empty(tagHelperOutput.Content.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.PostContent);
            Assert.Empty(tagHelperOutput.PostContent.GetContent(new HtmlTestEncoder()));
            Assert.NotNull(tagHelperOutput.PostElement);
            Assert.Empty(tagHelperOutput.PostElement.GetContent(new HtmlTestEncoder()));
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
                },
                (cachedResult) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

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