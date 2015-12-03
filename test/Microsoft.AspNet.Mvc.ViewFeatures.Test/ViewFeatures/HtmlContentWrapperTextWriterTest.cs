// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Html;
using Microsoft.AspNet.Mvc.Rendering;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewFeatures
{
    public class HtmlContentWrapperTextWriterTest
    {
        [Fact]
        public async Task Write_WritesCharBuffer()
        {
            // Arrange
            var input1 = new ArraySegment<char>(new char[] { 'a', 'b', 'c', 'd' }, 1, 3);
            var input2 = new ArraySegment<char>(new char[] { 'e', 'f' }, 0, 2);
            var input3 = new ArraySegment<char>(new char[] { 'g', 'h', 'i', 'j' }, 3, 1);
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(input1.Array, input1.Offset, input1.Count);
            await writer.WriteAsync(input2.Array, input2.Offset, input2.Count);
            await writer.WriteLineAsync(input3.Array, input3.Offset, input3.Count);

            // Assert
            Assert.Collection(buffer.Values,
                value => Assert.Equal("bcd", value),
                value => Assert.Equal("ef", value),
                value => Assert.Equal("j", value),
                value => Assert.Equal(Environment.NewLine, value));
        }

        [Fact]
        public void Write_SplitsCharBuffer_Into1kbStrings()
        {
            // Arrange
            var charArray = Enumerable.Range(0, 2050).Select(_ => 'a').ToArray();
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(charArray);

            // Assert
            Assert.Collection(
                buffer.Values,
                value => Assert.Equal(new string('a', 1024), value),
                value => Assert.Equal(new string('a', 1024), value),
                value => Assert.Equal("aa", value));
        }

        [Fact]
        public void Write_HtmlContent_AddsToEntries()
        {
            // Arrange
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write(content);

            // Assert
            Assert.Collection(
                buffer.Values,
                item => Assert.Same(content, item));
        }

        [Fact]
        public void Write_Object_HtmlContent_AddsToEntries()
        {
            // Arrange
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write((object)content);

            // Assert
            Assert.Collection(
                buffer.Values,
                item => Assert.Same(content, item));
        }

        [Fact]
        public void WriteLine_Object_HtmlContent_AddsToEntries()
        {
            // Arrange
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.WriteLine(content);

            // Assert
            Assert.Collection(
                buffer.Values,
                item => Assert.Same(content, item),
                item => Assert.Equal(Environment.NewLine, item));
        }

        [Fact]
        public async Task Write_WritesStringBuffer()
        {
            // Arrange
            var newLine = Environment.NewLine;
            var input1 = "Hello";
            var input2 = "from";
            var input3 = "ASP";
            var input4 = ".Net";
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(input1);
            writer.WriteLine(input2);
            await writer.WriteAsync(input3);
            await writer.WriteLineAsync(input4);

            // Assert
            Assert.Equal(new[] { input1, input2, newLine, input3, input4, newLine }, buffer.Values);
        }

        [Fact]
        public void Write_HtmlContent_WritesToBuffer()
        {
            // Arrange
            var buffer = new TestHtmlContentBuilder();
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write(content);

            // Assert
            Assert.Collection(
                buffer.Values,
                item => Assert.Same(content, item));
        }

        private class TestHtmlContentBuilder : IHtmlContentBuilder
        {
            public List<object> Values { get; } = new List<object>();

            public IHtmlContentBuilder Append(string unencoded)
            {
                Values.Add(unencoded);
                return this;
            }

            public IHtmlContentBuilder Append(IHtmlContent content)
            {
                Values.Add(content);
                return this;
            }

            public IHtmlContentBuilder AppendHtml(string encoded)
            {
                Values.Add(new HtmlString(encoded));
                return this;
            }

            public IHtmlContentBuilder Clear()
            {
                Values.Clear();
                return this;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                throw new NotSupportedException();
            }
        }
    }
}
