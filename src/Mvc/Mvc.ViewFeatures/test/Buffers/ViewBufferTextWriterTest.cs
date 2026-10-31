// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

public class ViewBufferTextWriterTest
{
    [Fact]
    [ReplaceCulture]
    public void Write_WritesDataTypes()
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
    public async Task Write_WritesDataTypes_AfterFlush()
    {
        // Arrange
        var expected = new object[] { "True", "3", "18446744073709551615", "Hello world", "3.14", "2.718", "m" };
        var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
        var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

        // Act
        await writer.FlushAsync();

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
    public void WriteLine_WritesDataTypes()
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
    public void WriteLine_WritesDataType_AfterFlush()
    {
        // Arrange
        var newLine = Environment.NewLine;
        var expected = new List<object> { "False", newLine, "1.1", newLine, "3", newLine };
        var inner = new Mock<TextWriter>();
        var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
        var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8, new HtmlTestEncoder(), inner.Object);

        // Act
        writer.Flush();
        writer.WriteLine(false);
        writer.WriteLine(1.1f);
        writer.WriteLine(3L);

        // Assert
        inner.Verify(v => v.Write("False"), Times.Never());
        inner.Verify(v => v.Write("1.1"), Times.Never());
        inner.Verify(v => v.Write("3"), Times.Never());
        inner.Verify(v => v.WriteLine(), Times.Never());

        Assert.Equal(expected, GetValues(buffer));
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
    public void Write_WritesEmptyCharBuffer()
    {
        // Arrange
        var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
        var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);
        var charBuffer = new char[0];

        // Act
        writer.Write(charBuffer, 0, 0);

        // Assert
        var actual = GetValues(buffer);
        Assert.Equal<object>(new[] { string.Empty }, actual);
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
