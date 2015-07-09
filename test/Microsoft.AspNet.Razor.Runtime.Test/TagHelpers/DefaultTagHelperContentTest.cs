// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.WebEncoders.Testing;
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

        // Overload with args array is called.
        [Fact]
        public void CanAppendFormatContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("{0} {1} {2} {3}!", "First", "Second", "Third", "Fourth");

            // Assert
            Assert.Equal("First Second Third Fourth!", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormatContent_With1Argument()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("0x{0, -5:X} - hex equivalent for 50.", 50);

            // Assert
            Assert.Equal("0x32    - hex equivalent for 50.", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormatContent_With2Arguments()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("0x{0, -5:X} - hex equivalent for {1}.", 50, 50);

            // Assert
            Assert.Equal("0x32    - hex equivalent for 50.", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormatContent_With3Arguments()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("0x{0, -5:X} - {1} equivalent for {2}.", 50, "hex", 50);

            // Assert
            Assert.Equal("0x32    - hex equivalent for 50.", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormat_WithAlignmentComponent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("{0, -10} World!", "Hello");

            // Assert
            Assert.Equal("Hello      World!", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormat_WithFormatStringComponent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("0x{0:X}", 50);

            // Assert
            Assert.Equal("0x32", tagHelperContent.GetContent());
        }

        // Overload with args array is called.
        [Fact]
        public void CanAppendFormat_WithCulture()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat(
                CultureInfo.InvariantCulture,
                "Numbers in InvariantCulture - {0, -5:N} {1} {2} {3}!",
                1.1,
                2.98,
                145.82,
                32.86);

            // Assert
            Assert.Equal("Numbers in InvariantCulture - 1.10  2.98 145.82 32.86!", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormat_WithCulture_1Argument()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat(
                CultureInfo.InvariantCulture,
                "Numbers in InvariantCulture - {0, -5:N}!",
                1.1);

            // Assert
            Assert.Equal("Numbers in InvariantCulture - 1.10 !", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormat_WithCulture_2Arguments()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat(
                CultureInfo.InvariantCulture,
                "Numbers in InvariantCulture - {0, -5:N} {1}!",
                1.1,
                2.98);

            // Assert
            Assert.Equal("Numbers in InvariantCulture - 1.10  2.98!", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormat_WithCulture_3Arguments()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat(
                CultureInfo.InvariantCulture,
                "Numbers in InvariantCulture - {0, -5:N} {1} {2}!",
                1.1,
                2.98,
                3.12);

            // Assert
            Assert.Equal("Numbers in InvariantCulture - 1.10  2.98 3.12!", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendFormat_WithDifferentCulture()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var culture = new CultureInfo("fr-FR");

            // Act
            tagHelperContent.AppendFormat(culture, "{0} in french!", 1.21);

            // Assert
            Assert.Equal("1,21 in french!", tagHelperContent.GetContent());
        }

        [Fact]
        [ReplaceCulture]
        public void CanAppendFormat_WithDifferentCurrentCulture()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat(CultureInfo.CurrentCulture, "{0:D}", DateTime.Parse("01/02/2015"));

            // Assert
            Assert.Equal("01 February 2015", tagHelperContent.GetContent());
        }

        [Fact]
        public void CanAppendDefaultTagHelperContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var helloWorldContent = new DefaultTagHelperContent();
            helloWorldContent.SetContent("HelloWorld");
            var expected = "Content was HelloWorld";

            // Act
            tagHelperContent.AppendFormat("Content was {0}", helloWorldContent);

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
        public void Fluent_SetContent_AppendFormat_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = new[] { "First ", "Second Third" };
            var i = 0;

            // Act
            tagHelperContent.SetContent("First ").AppendFormat("{0} Third", "Second");

            // Assert
            foreach (var value in tagHelperContent)
            {
                Assert.Equal(expected[i++], value);
            }
        }

        [Fact]
        public void Fluent_SetContent_AppendFormat_Append_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = new[] { "First ", "Second Third ", "Fourth" };
            var i = 0;

            // Act
            tagHelperContent
                .SetContent("First ")
                .AppendFormat("{0} Third ", "Second")
                .Append("Fourth");

            // Assert
            foreach (var value in tagHelperContent)
            {
                Assert.Equal(expected[i++], value);
            }
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

        [Fact]
        public void WriteTo_WritesToATextWriter()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var writer = new StringWriter();
            tagHelperContent.SetContent("Hello ");
            tagHelperContent.Append("World");

            // Act
            tagHelperContent.WriteTo(writer, new CommonTestEncoder());

            // Assert
            Assert.Equal("Hello World", writer.ToString());
        }
    }
}
