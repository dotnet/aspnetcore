// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class DefaultTagHelperContentTest
    {
        [Fact]
        public void Reset_ClearsTheExpectedFields()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.SetContent("hello world");

            // Act
            tagHelperContent.Reinitialize();

            Assert.False(tagHelperContent.IsModified);
            Assert.Equal(string.Empty, tagHelperContent.GetContent());
        }

        [Fact]
        public void CanSetContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[Hello World!]]";

            // Act
            tagHelperContent.SetContent("Hello World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void SetContentClearsExistingContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[Hello World!]]";
            tagHelperContent.SetContent("Contoso");

            // Act
            tagHelperContent.SetContent("Hello World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void SetHtmlContent_TextIsNotFurtherEncoded()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetHtmlContent("Hi");

            // Assert
            Assert.Equal("Hi", tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void SetHtmlContent_ClearsExistingContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.AppendHtml("Contoso");

            // Act
            tagHelperContent.SetHtmlContent("Hello World!");

            // Assert
            Assert.Equal("Hello World!", tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Theory]
        [InlineData("HelloWorld!", "HtmlEncode[[HelloWorld!]]")]
        [InlineData("  ", "HtmlEncode[[  ]]")]
        public void SetHtmlContent_WithTagHelperContent_WorksAsExpected(string content, string expected)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.SetContent(content);

            // Act
            copiedTagHelperContent.SetHtmlContent(tagHelperContent);

            // Assert
            Assert.Equal(expected, copiedTagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void CopyTo_CopiesAllItems()
        {
            // Arrange
            var source = new DefaultTagHelperContent();
            source.AppendHtml(new HtmlString("hello"));
            source.Append("Test");

            var items = new List<object>();
            var destination = new HtmlContentBuilder(items);
            destination.Append("some-content");

            // Act
            source.CopyTo(destination);

            // Assert
            Assert.Equal(3, items.Count);

            Assert.Equal("some-content", Assert.IsType<string>(items[0]));
            Assert.Equal("hello", Assert.IsType<HtmlString>(items[1]).Value);
            Assert.Equal("Test", Assert.IsType<string>(items[2]));
        }

        [Fact]
        public void CopyTo_DoesDeepCopy()
        {
            // Arrange
            var source = new DefaultTagHelperContent();

            var nested = new DefaultTagHelperContent();
            source.AppendHtml(nested);
            nested.AppendHtml(new HtmlString("hello"));
            source.Append("Test");

            var items = new List<object>();
            var destination = new HtmlContentBuilder(items);
            destination.Append("some-content");

            // Act
            source.CopyTo(destination);

            // Assert
            Assert.Equal(3, items.Count);

            Assert.Equal("some-content", Assert.IsType<string>(items[0]));
            Assert.Equal("hello", Assert.IsType<HtmlString>(items[1]).Value);
            Assert.Equal("Test", Assert.IsType<string>(items[2]));
        }

        [Fact]
        public void MoveTo_CopiesAllItems_AndClears()
        {
            // Arrange
            var source = new DefaultTagHelperContent();
            source.AppendHtml(new HtmlString("hello"));
            source.Append("Test");

            var items = new List<object>();
            var destination = new HtmlContentBuilder(items);
            destination.Append("some-content");

            // Act
            source.MoveTo(destination);

            // Assert
            Assert.Equal(string.Empty, source.GetContent());
            Assert.Equal(3, items.Count);

            Assert.Equal("some-content", Assert.IsType<string>(items[0]));
            Assert.Equal("hello", Assert.IsType<HtmlString>(items[1]).Value);
            Assert.Equal("Test", Assert.IsType<string>(items[2]));
        }

        [Fact]
        public void MoveTo_DoesDeepMove()
        {
            // Arrange
            var source = new DefaultTagHelperContent();

            var nested = new DefaultTagHelperContent();
            source.AppendHtml(nested);
            nested.AppendHtml(new HtmlString("hello"));
            source.Append("Test");

            var items = new List<object>();
            var destination = new HtmlContentBuilder(items);
            destination.Append("some-content");

            // Act
            source.MoveTo(destination);

            // Assert
            Assert.Equal(string.Empty, source.GetContent());
            Assert.Equal(string.Empty, nested.GetContent());
            Assert.Equal(3, items.Count);

            Assert.Equal("some-content", Assert.IsType<string>(items[0]));
            Assert.Equal("hello", Assert.IsType<HtmlString>(items[1]).Value);
            Assert.Equal("Test", Assert.IsType<string>(items[2]));
        }

        // GetContent - this one relies on the default encoder.
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
            var expected = "HtmlEncode[[Hello World!]]";

            // Act
            tagHelperContent.Append("Hello World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void CanAppendFormatContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat("{0} {1} {2} {3}!", "First", "Second", "Third", "Fourth");

            // Assert
            Assert.Equal(
                "HtmlEncode[[First]] HtmlEncode[[Second]] HtmlEncode[[Third]] HtmlEncode[[Fourth]]!",
                tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void CanAppendFormat_WithCulture()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendFormat(
                CultureInfo.InvariantCulture,
                "Numbers in InvariantCulture - {0:N} {1} {2} {3}!",
                1.1,
                2.98,
                145.82,
                32.86);

            // Assert
            Assert.Equal(
                "Numbers in InvariantCulture - HtmlEncode[[1.10]] HtmlEncode[[2.98]] " +
                    "HtmlEncode[[145.82]] HtmlEncode[[32.86]]!",
                tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void CanAppendDefaultTagHelperContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var helloWorldContent = new DefaultTagHelperContent();
            helloWorldContent.SetContent("HelloWorld");
            var expected = "Content was HtmlEncode[[HelloWorld]]";

            // Act
            tagHelperContent.AppendFormat("Content was {0}", helloWorldContent);

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void Append_WithTagHelperContent_MultipleAppends()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            var text1 = "Hello";
            var text2 = " World!";
            var expected = "HtmlEncode[[Hello]]HtmlEncode[[ World!]]";
            tagHelperContent.Append(text1);
            tagHelperContent.Append(text2);

            // Act
            copiedTagHelperContent.AppendHtml(tagHelperContent);

            // Assert
            Assert.Equal(expected, copiedTagHelperContent.GetContent(new HtmlTestEncoder()));
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
            tagHelperContent.AppendHtml(NullContent);

            // Assert
            Assert.True(tagHelperContent.IsModified);
        }

        public static TheoryData<string> EmptyOrWhiteSpaceData
        {
            get
            {
                return new TheoryData<string>
                {
                    string.Empty,
                    " ",
                    "\n",
                    "\t",
                    "\r",
                    "\r\n",
                    "\u2000",
                    "\u205f",
                    "\u3000",
                    " \u200a \t",
                };
            }
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterSetContent(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent(data);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_FalseAfterLaterAppend()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent("  ");
            tagHelperContent.Append("Hello");

            // Assert
            Assert.True(tagHelperContent.GetContent().Length > 0);
            Assert.False(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_InitiallyTrue()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act & Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppend(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append(data);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTwice(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append(data);
            tagHelperContent.Append(data);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendHtml(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendHtml(data);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendHtmlTwice(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendHtml(data);
            tagHelperContent.AppendHtml(data);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendEmptyTagHelperContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendEmptyTagHelperContentTwice()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTagHelperContent(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            copiedTagHelperContent.AppendHtml(data);

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTagHelperContentTwice(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            copiedTagHelperContent.AppendHtml(data);

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTagHelperContent_WithDataToEncode(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            copiedTagHelperContent.Append(data);

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Theory]
        [MemberData(nameof(EmptyOrWhiteSpaceData))]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTagHelperContentTwice_WithDataToEncode(string data)
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            copiedTagHelperContent.Append(data);

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTagHelperContent_WithCharByCharWriteTo()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new CharByCharWhiteSpaceHtmlContent();

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_TrueAfterAppendTagHelperContentTwice_WithCharByCharWriteTo()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new CharByCharWhiteSpaceHtmlContent();

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_FalseAfterAppendTagHelperContentTwice_WithCharByCharWriteTo()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new CharByCharNonWhiteSpaceHtmlContent();

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.False(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_TrueAfterClear()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Clear();

            // Assert
            Assert.Equal(string.Empty, tagHelperContent.GetContent());
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_FalseAfterSetContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.SetContent("Hello");

            // Assert
            Assert.False(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_FalseAfterAppend()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();

            // Act
            tagHelperContent.Append("Hello");

            // Assert
            Assert.False(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void IsEmptyOrWhiteSpace_FalseAfterAppendTagHelperContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var copiedTagHelperContent = new DefaultTagHelperContent();
            copiedTagHelperContent.SetContent("Hello");

            // Act
            tagHelperContent.AppendHtml(copiedTagHelperContent);

            // Assert
            Assert.False(tagHelperContent.IsEmptyOrWhiteSpace);
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
            Assert.True(tagHelperContent.IsEmptyOrWhiteSpace);
        }

        [Fact]
        public void Fluent_SetContent_Append_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[Hello ]]HtmlEncode[[World!]]";

            // Act
            tagHelperContent.SetContent("Hello ").Append("World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void Fluent_SetContent_AppendFormat_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[First ]]HtmlEncode[[Second]] Third";

            // Act
            tagHelperContent.SetContent("First ").AppendFormat("{0} Third", "Second");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void Fluent_SetContent_AppendFormat_Append_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[First ]]HtmlEncode[[Second]] Third HtmlEncode[[Fourth]]";

            // Act
            tagHelperContent
                .SetContent("First ")
                .AppendFormat("{0} Third ", "Second")
                .Append("Fourth");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void Fluent_Clear_SetContent_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[Hello World]]";
            tagHelperContent.SetContent("Some Random Content");

            // Act
            tagHelperContent.Clear().SetContent("Hello World");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
        }

        [Fact]
        public void Fluent_Clear_Set_Append_WritesExpectedContent()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            var expected = "HtmlEncode[[Hello ]]HtmlEncode[[World!]]";
            tagHelperContent.SetContent("Some Random Content");

            // Act
            tagHelperContent.Clear().SetContent("Hello ").Append("World!");

            // Assert
            Assert.Equal(expected, tagHelperContent.GetContent(new HtmlTestEncoder()));
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
            tagHelperContent.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hello ]]HtmlEncode[[World]]", writer.ToString());
        }

        [Fact]
        public void Append_WrittenAsEncoded()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.Append("Hi");

            var writer = new StringWriter();

            // Act
            tagHelperContent.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hi]]", writer.ToString());
        }

        [Fact]
        public void AppendHtml_DoesNotGetEncoded()
        {
            // Arrange
            var tagHelperContent = new DefaultTagHelperContent();
            tagHelperContent.AppendHtml("Hi");

            var writer = new StringWriter();

            // Act
            tagHelperContent.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("Hi", writer.ToString());
        }

        private class CharByCharWhiteSpaceHtmlContent : IHtmlContent
        {
            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                writer.Write(' ');
                writer.Write('\n');
                writer.Write('\t');
                writer.Write('\r');
                writer.Write('\u2000');
                writer.Write('\u205f');
                writer.Write('\u3000');
            }
        }

        private class CharByCharNonWhiteSpaceHtmlContent : IHtmlContent
        {
            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                writer.Write('\u2000');
                writer.Write('h');
                writer.Write('e');
                writer.Write('l');
                writer.Write('l');
                writer.Write('o');
                writer.Write('\u200a');
                writer.Write('É');
                writer.Write('r');
                writer.Write('i');
                writer.Write('c');
                writer.Write('\u3000');
            }
        }
    }
}