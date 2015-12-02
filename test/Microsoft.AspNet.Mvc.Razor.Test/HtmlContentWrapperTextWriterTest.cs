// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Razor.Buffer;
using Microsoft.AspNet.Mvc.Rendering;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
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
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(input1.Array, input1.Offset, input1.Count);
            await writer.WriteAsync(input2.Array, input2.Offset, input2.Count);
            await writer.WriteLineAsync(input3.Array, input3.Offset, input3.Count);

            // Assert
            var bufferValues = GetValues(buffer);
            Assert.Equal(4, bufferValues.Length);
            Assert.Equal("bcd", bufferValues[0]);
            Assert.Equal("ef", bufferValues[1]);
            Assert.Equal("j", bufferValues[2]);
            Assert.Equal(Environment.NewLine, bufferValues[3]);
        }

        [Fact]
        public void Write_SplitsCharBuffer_Into1kbStrings()
        {
            // Arrange
            var charArray = Enumerable.Range(0, 2050).Select(_ => 'a').ToArray();
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(charArray);

            // Assert
            Assert.Collection(GetValues(buffer),
                value => Assert.Equal(new string('a', 1024), value),
                value => Assert.Equal(new string('a', 1024), value),
                value => Assert.Equal("aa", value));
        }

        [Fact]
        public void Write_HtmlContent_AddsToEntries()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write(content);

            // Assert
            Assert.Collection(
                GetValues(buffer),
                item => Assert.Same(content, item));
        }

        [Fact]
        public void Write_Object_HtmlContent_AddsToEntries()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write((object)content);

            // Assert
            Assert.Collection(
                GetValues(buffer),
                item => Assert.Same(content, item));
        }

        [Fact]
        public void WriteLine_Object_HtmlContent_AddsToEntries()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.WriteLine(content);

            // Assert
            Assert.Collection(
                GetValues(buffer),
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
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(input1);
            writer.WriteLine(input2);
            await writer.WriteAsync(input3);
            await writer.WriteLineAsync(input4);

            // Assert
            var actual = GetValues(buffer);
            Assert.Equal(new object[] { input1, input2, newLine, input3, input4, newLine }, actual);
        }

        [Fact]
        public void Write_HtmlContent_WritesToBuffer()
        {
            // Arrange
            var buffer = new RazorBuffer(new TestRazorBufferScope(), "some-name");
            var writer = new HtmlContentWrapperTextWriter(buffer, Encoding.UTF8);
            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write(content);

            // Assert
            Assert.Collection(
                GetValues(buffer),
                item => Assert.Same(content, item));
        }

        private static object[] GetValues(RazorBuffer buffer)
        {
            return buffer.BufferSegments
                .SelectMany(c => c.Data)
                .Select(d => d.Value)
                .TakeWhile(d => d != null)
                .ToArray();
        }
    }
}
