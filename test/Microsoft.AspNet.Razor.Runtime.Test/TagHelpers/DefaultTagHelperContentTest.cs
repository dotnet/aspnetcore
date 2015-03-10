// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class DefaultTagHelperContentTest
    {
        [Fact]
        public void CanSetContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";

            // Act
            tagHelperContent.SetContent(expected);

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent());
        }

        [Fact]
        public void SetContentClearsExistingContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";
            tagHelperContent.SetContent("Contoso");

            // Act
            tagHelperContent.SetContent(expected);

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent());
        }

        [Theory]
        [InlineData("HelloWorld!")]
        [InlineData("  ")]
        public void SetContent_WithTagHelperContent_WorksAsExpected(string expected)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.SetContent(expected);

            // Act
            copiedTagHelperContent.SetContent(tagHelperContent);

            // Assert
            Assert.Equal(expected, copiedTagHelperContent.GetContent());
        }

        // GetContent
        [Fact]
        public void CanGetContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";
            tagHelperContent.SetContent(expected);

            // Act
            var actual = tagHelperContent.GetContent();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanAppendContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";

            // Act
            tagHelperContent.Append(expected);

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent());
        }

        [Fact]
        public void Append_WithTagHelperContent_MultipleAppends()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            var text1 = "Hello";
            var text2 = " World!";
            var expected = text1 + text2;
            tagHelperContent.Append(text1);
            tagHelperContent.Append(text2);

            // Act
            copiedTagHelperContent.Append(tagHelperContent);

            // Assert
            Assert.Equal(expected, copiedTagHelperContent.GetContent());
            Assert.Equal(new[] { text1, text2 }, copiedTagHelperContent.ToArray());
        }

        [Fact]
        public void IsModified_TrueAfterSetContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent(string.Empty);

            // Assert
            Assert.True(tagHelperContent.IsModified);
        }

        
        [Fact]
        public void IsModified_TrueAfterAppend()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append(string.Empty);

            // Assert
            Assert.True(tagHelperContent.IsModified);
        }

        [Fact]
        public void IsModified_TrueAfterClear()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Clear();

            // Assert
            Assert.True(tagHelperContent.IsModified);
        }

        [Fact]
        public void IsModified_TrueIfAppendedNull()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            TagHelperContent NullContent = null;

            // Act
            tagHelperContent.Append(NullContent);

            // Assert
            Assert.True(tagHelperContent.IsModified);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData("\t")]
        [InlineData("\r")]
        public void CanIdentifyWhiteSpace(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent("  ");
            tagHelperContent.Append(data);

            // Assert
            Assert.True(tagHelperContent.IsWhiteSpace);
        }

        [Fact]
        public void CanIdentifyWhiteSpace_WithoutIgnoringStrings()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent("  ");
            tagHelperContent.Append("Hello");

            // Assert
            Assert.False(tagHelperContent.IsWhiteSpace);
        }

        [Fact]
        public void IsEmpty_InitiallyTrue()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act & Assert
            Assert.True(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_TrueAfterSetEmptyContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent(string.Empty);

            // Assert
            Assert.True(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_TrueAfterAppendEmptyContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append(string.Empty);
            tagHelperContent.Append(string.Empty);

            // Assert
            Assert.True(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_TrueAfterAppendEmptyTagHelperContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append(copiedTagHelperContent);
            tagHelperContent.Append(string.Empty);

            // Assert
            Assert.True(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_TrueAfterClear()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Clear();

            // Assert
            Assert.True(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_FalseAfterSetContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent("Hello");

            // Assert
            Assert.False(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_FalseAfterAppend()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append("Hello");

            // Assert
            Assert.False(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void IsEmpty_FalseAfterAppendTagHelper()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            copiedTagHelperContent.SetContent("Hello");

            // Act
            tagHelperContent.Append(copiedTagHelperContent);

            // Assert
            Assert.False(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void CanClearContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.SetContent("Hello World");

            // Act
            tagHelperContent.Clear();

            // Assert
            Assert.True(tagHelperContent.IsEmpty);
        }

        [Fact]
        public void ToString_ReturnsExpectedValue()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";
            tagHelperContent.SetContent(expected);

            // Act
            var actual = tagHelperContent.ToString();

            // Assert
            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }

        [Fact]
        public void GetEnumerator_EnumeratesThroughBuffer()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = new string[] { "Hello", "World" };
            tagHelperContent.SetContent(expected[0]);
            tagHelperContent.Append(expected[1]);
            var i = 0;

            // Act & Assert
            foreach (var val in tagHelperContent)
            {
                Assert.Equal(expected[i++], val);
            }
        }

        [Fact]
        public void Fluent_SetContent_Append_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";

            // Act
            tagHelperContent.SetContent("Hello ").Append("World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent());
        }

        [Fact]
        public void Fluent_Clear_SetContent_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";
            tagHelperContent.SetContent("Some Random Content");

            // Act
            tagHelperContent.Clear().SetContent(expected);

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent());
        }

        [Fact]
        public void Fluent_Clear_Set_Append_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "Hello World!";
            tagHelperContent.SetContent("Some Random Content");

            // Act
            tagHelperContent.Clear().SetContent("Hello ").Append("World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent());
        }
    }
}
