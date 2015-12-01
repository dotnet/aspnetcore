// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Html;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class HtmlContentBuilderTest
    {
        [Fact]
        public void AppendString_AppendsAString()
        {
            // Arrange
            var content = new HtmlContentBuilder();

            // Act
            content.Append("Hello");

            // Assert
            var result = Assert.Single(content.Entries);
            Assert.IsType<string>(result);
        }

        [Fact]
        public void AppendString_WrittenAsEncoded()
        {
            // Arrange
            var content = new HtmlContentBuilder();
            content.Append("Hello");

            var writer = new StringWriter();

            // Act
            content.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hello]]", writer.ToString());
        }

        [Fact]
        public void AppendHtml_DoesNotGetWrittenAsEncoded()
        {
            // Arrange
            var content = new HtmlContentBuilder();
            content.AppendHtml("Hello");

            var writer = new StringWriter();

            // Act
            content.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("Hello", writer.ToString());
        }

        [Fact]
        public void AppendIHtmlContent_AppendsAsIs()
        {
            // Arrange
            var content = new HtmlContentBuilder();
            var writer = new StringWriter();

            // Act
            content.Append(new TestHtmlContent("Hello"));

            // Assert
            var result = Assert.Single(content.Entries);
            var testHtmlContent = Assert.IsType<TestHtmlContent>(result);
            testHtmlContent.WriteTo(writer, new HtmlTestEncoder());
            Assert.Equal("Written from TestHtmlContent: Hello", writer.ToString());
        }

        [Fact]
        public void CanAppendMultipleItems()
        {
            // Arrange
            var content = new HtmlContentBuilder();

            // Act
            content.Append(new TestHtmlContent("hello"));
            content.Append("Test");

            // Assert
            Assert.Equal(2, content.Entries.Count);
            Assert.Equal("Written from TestHtmlContent: hello", content.Entries[0].ToString());
            Assert.Equal("Test", content.Entries[1]);
        }

        [Fact]
        public void Clear_DeletesAllItems()
        {
            // Arrange
            var content = new HtmlContentBuilder();
            content.Append(new TestHtmlContent("hello"));
            content.Append("Test");

            // Act
            content.Clear();

            // Assert
            Assert.Equal(0, content.Entries.Count);
        }

        [Fact]
        public void WriteTo_WritesAllItems()
        {
            // Arrange
            var content = new HtmlContentBuilder();
            var writer = new StringWriter();
            content.Append(new TestHtmlContent("Hello"));
            content.Append("Test");

            // Act
            content.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal(2, content.Entries.Count);
            Assert.Equal("Written from TestHtmlContent: HelloHtmlEncode[[Test]]", writer.ToString());
        }

        private class TestHtmlContent : IHtmlContent
        {
            private string _content;

            public TestHtmlContent(string content)
            {
                _content = content;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                writer.Write(ToString());
            }

            public override string ToString()
            {
                return "Written from TestHtmlContent: " + _content;
            }
        }
    }
}
