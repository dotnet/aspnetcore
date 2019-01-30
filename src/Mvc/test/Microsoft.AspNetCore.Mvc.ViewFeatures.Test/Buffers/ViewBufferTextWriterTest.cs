// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers
{
    public class ViewBufferTextWriterTest
    {
        [Fact]
        [ReplaceCulture]
        public void Write_WritesDataTypes_ToBuffer()
        {
            // Arrange
            var expected = new object[] { "True", "3", "18446744073709551615", "Hello world", "3.14", "2.718", "m" };
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(true);
            writer.Write(3);
            writer.Write(ulong.MaxValue);
            writer.Write(new TestClass());
            writer.Write(3.14);
            writer.Write(2.718m);
            writer.Write('m');

            // Assert
            Assert.Equal(expected, GetValues(buffer));
        }

        [Fact]
        [ReplaceCulture]
        public void Write_WritesDataTypes_ToUnderlyingStream_WhenNotBuffering()
        {
            // Arrange
            var expected = new[] { "True", "3", "18446744073709551615", "Hello world", "3.14", "2.718" };
            var inner = new Mock<TextWriter>();
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8, new HtmlTestEncoder(), inner.Object);
            var testClass = new TestClass();

            // Act
            writer.Flush();
            writer.Write(true);
            writer.Write(3);
            writer.Write(ulong.MaxValue);
            writer.Write(testClass);
            writer.Write(3.14);
            writer.Write(2.718m);

            // Assert
            Assert.Equal(0, buffer.Count);
            foreach (var item in expected)
            {
                inner.Verify(v => v.Write(item), Times.Once());
            }
        }

        [Fact]
        [ReplaceCulture]
        public async Task Write_WritesCharValues_ToUnderlyingStream_WhenNotBuffering()
        {
            // Arrange
            var inner = new Mock<TextWriter> { CallBase = true };
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8, new HtmlTestEncoder(), inner.Object);
            var buffer1 = new[] { 'a', 'b', 'c', 'd' };
            var buffer2 = new[] { 'd', 'e', 'f' };

            // Act
            writer.Flush();
            writer.Write('x');
            writer.Write(buffer1, 1, 2);
            writer.Write(buffer2);
            await writer.WriteAsync(buffer2, 1, 1);
            await writer.WriteLineAsync(buffer1);

            // Assert
            inner.Verify(v => v.Write('x'), Times.Once());
            inner.Verify(v => v.Write(buffer1, 1, 2), Times.Once());
            inner.Verify(v => v.Write(buffer1, 0, 4), Times.Once());
            inner.Verify(v => v.Write(buffer2, 0, 3), Times.Once());
            inner.Verify(v => v.WriteAsync(buffer2, 1, 1), Times.Once());
            inner.Verify(v => v.WriteLine(), Times.Once());
        }

        [Fact]
        [ReplaceCulture]
        public async Task Write_WritesStringValues_ToUnbufferedStream_WhenNotBuffering()
        {
            // Arrange
            var inner = new Mock<TextWriter>();
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8, new HtmlTestEncoder(), inner.Object);

            // Act
            await writer.FlushAsync();
            writer.Write("a");
            writer.WriteLine("ab");
            await writer.WriteAsync("ef");
            await writer.WriteLineAsync("gh");

            // Assert
            inner.Verify(v => v.Write("a"), Times.Once());
            inner.Verify(v => v.WriteLine("ab"), Times.Once());
            inner.Verify(v => v.WriteAsync("ef"), Times.Once());
            inner.Verify(v => v.WriteLineAsync("gh"), Times.Once());
        }

        [Fact]
        [ReplaceCulture]
        public void WriteLine_WritesDataTypes_ToBuffer()
        {
            // Arrange
            var newLine = Environment.NewLine;
            var expected = new List<object> { "False", newLine, "1.1", newLine, "3", newLine };
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.WriteLine(false);
            writer.WriteLine(1.1f);
            writer.WriteLine(3L);

            // Assert
            Assert.Equal(expected, GetValues(buffer));
        }

        [Fact]
        [ReplaceCulture]
        public void WriteLine_WritesDataTypes_ToUnbufferedStream_WhenNotBuffering()
        {
            // Arrange
            var inner = new Mock<TextWriter>();
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8, new HtmlTestEncoder(), inner.Object);

            // Act
            writer.Flush();
            writer.WriteLine(false);
            writer.WriteLine(1.1f);
            writer.WriteLine(3L);

            // Assert
            inner.Verify(v => v.Write("False"), Times.Once());
            inner.Verify(v => v.Write("1.1"), Times.Once());
            inner.Verify(v => v.Write("3"), Times.Once());
            inner.Verify(v => v.WriteLine(), Times.Exactly(3));
        }

        [Fact]
        public async Task WriteLines_WritesCharBuffer()
        {
            // Arrange
            var newLine = Environment.NewLine;
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.WriteLine();
            await writer.WriteLineAsync();

            // Assert
            var actual = GetValues(buffer);
            Assert.Equal<object>(new[] { newLine, newLine }, actual);
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
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

            // Act
            writer.Write(input1);
            writer.WriteLine(input2);
            await writer.WriteAsync(input3);
            await writer.WriteLineAsync(input4);

            // Assert
            var actual = GetValues(buffer);
            Assert.Equal<object>(new[] { input1, input2, newLine, input3, input4, newLine }, actual);
        }

        [Fact]
        public void Write_HtmlContent_AfterFlush_GoesToStream()
        {
            // Arrange
            var inner = new StringWriter();
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8, new HtmlTestEncoder(), inner);

            writer.Flush();

            var content = new HtmlString("Hello, world!");

            // Act
            writer.Write(content);

            // Assert
            Assert.Equal("Hello, world!", inner.ToString());
        }

        private static object[] GetValues(ViewBuffer buffer)
        {
            var pages = new List<ViewBufferPage>();
            for (var i = 0; i < buffer.Count; i++)
            {
                pages.Add(buffer[i]);
            }

            return pages
                .SelectMany(c => c.Buffer)
                .Select(d => d.Value)
                .TakeWhile(d => d != null)
                .ToArray();
        }

        private class TestClass
        {
            public override string ToString()
            {
                return "Hello world";
            }
        }
    }
}