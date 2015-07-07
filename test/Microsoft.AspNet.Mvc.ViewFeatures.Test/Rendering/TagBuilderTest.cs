// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Rendering
{
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
        [InlineData(true, "Hello", "something else")]
        public void MergeAttribute_IgnoresCase(bool replaceExisting, string expectedKey, string expectedValue)
        {
            // Arrange
            var tagBuilder = new TagBuilder("p", new NullTestEncoder());
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
            var tagBuilder = new TagBuilder("p", new NullTestEncoder());
            tagBuilder.Attributes.Add("ClaSs", "btn");

            // Act
            tagBuilder.AddCssClass("success");

            // Assert
            var attribute = Assert.Single(tagBuilder.Attributes);
            Assert.Equal(new KeyValuePair<string, string>("ClaSs", "success btn"), attribute);
        }

        [Fact]
        public void GenerateId_IgnoresCase()
        {
            // Arrange
            var tagBuilder = new TagBuilder("p", new NullTestEncoder());
            tagBuilder.Attributes.Add("ID", "something");

            // Act
            tagBuilder.GenerateId("else", idAttributeDotReplacement: "-");

            // Assert
            var attribute = Assert.Single(tagBuilder.Attributes);
            Assert.Equal(new KeyValuePair<string, string>("ID", "something"), attribute);
        }

        [Theory]
        [MemberData(nameof(RenderingTestingData))]
        public void ToString_IgnoresIdAttributeCase(TagRenderMode renderingMode, string expectedOutput)
        {
            // Arrange
            var tagBuilder = new TagBuilder("p", new NullTestEncoder());

            // An empty value id attribute should not be rendered via ToString.
            tagBuilder.Attributes.Add("ID", string.Empty);

            // Act
            var value = tagBuilder.ToString(renderingMode);

            // Assert
            Assert.Equal(expectedOutput, value);
        }

        [Theory]
        [MemberData(nameof(RenderingTestingData))]
        public void ToHtmlString_IgnoresIdAttributeCase(TagRenderMode renderingMode, string expectedOutput)
        {
            // Arrange
            var tagBuilder = new TagBuilder("p", new NullTestEncoder());

            // An empty value id attribute should not be rendered via ToHtmlString.
            tagBuilder.Attributes.Add("ID", string.Empty);

            // Act
            var value = tagBuilder.ToHtmlString(renderingMode);

            // Assert
            Assert.Equal(expectedOutput, value.ToString());
        }

        [Fact]
        public void SetInnerText_HtmlEncodesValue()
        {
            // Arrange
            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());

            // Act
            tagBuilder.SetInnerText("TestValue");

            // Assert
            Assert.Equal("HtmlEncode[[TestValue]]", tagBuilder.InnerHtml);
        }

        [Theory]
        [InlineData("HelloWorld", "HelloWorld")]
        [InlineData("¡HelloWorld", "zHelloWorld")]
        [InlineData("Hello¡World", "Hello-World")]
        public void CreateSanitizedIdCreatesId(string input, string output)
        {
            // Arrange
            var result = TagBuilder.CreateSanitizedId(input, "-");

            // Assert
            Assert.Equal(output, result);
        }
    }
}