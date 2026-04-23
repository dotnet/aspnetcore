// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

public class PagedBufferedTextWriterTest
{
    private static readonly char[] Content;

    static PagedBufferedTextWriterTest()
    {
        Content = new char[4 * PagedCharBuffer.PageSize];
        for (var i = 0; i < Content.Length; i++)
        {
            Content[i] = (char)((i % 26) + 'A');
        }
    }

    [Fact]
    public async Task Write_Char()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // Act
        for (var i = 0; i < Content.Length; i++)
        {
            writer.Write(Content[i]);
        }

        await writer.FlushAsync();

        // Assert
        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    [Fact]
    public async Task Write_CharArray_Null()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // Act
        writer.Write((char[])null);

        await writer.FlushAsync();

        // Assert
        Assert.Empty(inner.ToString());
    }

    [Fact]
    public async Task Write_CharArray()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // These numbers chosen to hit boundary conditions in buffer lengths
        Assert.Equal(4096, Content.Length); // Update these numbers if this changes.
        var chunkSizes = new int[] { 3, 1021, 1023, 1023, 1, 1, 1024 };

        // Act
        var offset = 0;
        foreach (var chunkSize in chunkSizes)
        {
            var chunk = new char[chunkSize];
            for (var j = 0; j < chunkSize; j++)
            {
                chunk[j] = Content[offset + j];
            }

            writer.Write(chunk);
            offset += chunkSize;
        }

        await writer.FlushAsync();

        // Assert
        var array = inner.ToString().ToCharArray();
        for (var i = 0; i < Content.Length; i++)
        {
            Assert.Equal(Content[i], array[i]);
        }

        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    [Fact]
    public void Write_CharArray_Bounded_Null()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // Act & Assert
        Assert.Throws<ArgumentNullException>("buffer", () => writer.Write(null, 0, 0));
    }

    [Fact]
    public async Task Write_CharArray_Bounded()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // These numbers chosen to hit boundary conditions in buffer lengths
        Assert.Equal(4096, Content.Length); // Update these numbers if this changes.
        var chunkSizes = new int[] { 3, 1021, 1023, 1023, 1, 1, 1024 };

        // Act
        var offset = 0;
        foreach (var chunkSize in chunkSizes)
        {
            writer.Write(Content, offset, chunkSize);
            offset += chunkSize;
        }

        await writer.FlushAsync();

        // Assert
        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    [Fact]
    public async Task Write_String_Null()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // Act
        writer.Write((string)null);

        await writer.FlushAsync();

        // Assert
        Assert.Empty(inner.ToString());
    }

    [Fact]
    public async Task Write_String()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        // These numbers chosen to hit boundary conditions in buffer lengths
        Assert.Equal(4096, Content.Length); // Update these numbers if this changes.
        var chunkSizes = new int[] { 3, 1021, 1023, 1023, 1, 1, 1024 };

        // Act
        var offset = 0;
        foreach (var chunkSize in chunkSizes)
        {
            var chunk = new string(Content, offset, chunkSize);
            writer.Write(chunk);
            offset += chunkSize;
        }

        await writer.FlushAsync();

        // Assert
        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    [Fact]
    public async Task SynchronousWrites_FollowedByAsyncWriteString_WritesAllContent()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        // Act
        writer.Write('a');
        writer.Write(new[] { 'b', 'c', 'd' });
        writer.Write("ef");
        await writer.WriteAsync("ghi");

        // Assert
        Assert.Equal("abcdefghi", inner.ToString());
    }

    [Fact]
    public async Task SynchronousWrites_FollowedByAsyncWriteChar_WritesAllContent()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        // Act
        writer.Write('a');
        writer.Write(new[] { 'b', 'c', 'd' });
        writer.Write("ef");
        await writer.WriteAsync('g');

        // Assert
        Assert.Equal("abcdefg", inner.ToString());
    }

    [Fact]
    public async Task SynchronousWrites_FollowedByAsyncWriteCharArray_WritesAllContent()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        // Act
        writer.Write('a');
        writer.Write(new[] { 'b', 'c', 'd' });
        writer.Write("ef");
        await writer.WriteAsync(new[] { 'g', 'h', 'i' });

        // Assert
        Assert.Equal("abcdefghi", inner.ToString());
    }

    [Fact]
    public async Task FlushAsync_ReturnsPages()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);

        for (var i = 0; i < Content.Length; i++)
        {
            writer.Write(Content[i]);
        }

        // Act
        await writer.FlushAsync();

        // Assert
        Assert.Equal(3, pool.Returned.Count);
    }

    [Fact]
    public async Task FlushAsync_FlushesContent()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);
        for (var i = 0; i < Content.Length; i++)
        {
            writer.Write(Content[i]);
        }

        // Act
        await writer.FlushAsync();

        // Assert
        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    [Fact]
    public async Task FlushAsync_WritesContentToInner()
    {
        // Arrange
        var pool = new TestArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);
        for (var i = 0; i < Content.Length; i++)
        {
            writer.Write(Content[i]);
        }

        // Act
        await writer.FlushAsync();

        // Assert
        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    [Fact]
    public async Task FlushAsync_WritesContentToInner_WithLargeArrays()
    {
        // Arrange
        var pool = new RentMoreArrayPool();
        var inner = new StringWriter();

        var writer = new PagedBufferedTextWriter(pool, inner);
        for (var i = 0; i < Content.Length; i++)
        {
            writer.Write(Content[i]);
        }

        // Act
        await writer.FlushAsync();

        // Assert
        Assert.Equal<char>(Content, inner.ToString().ToCharArray());
    }

    private class TestArrayPool : ArrayPool<char>
    {
        public IList<char[]> Returned { get; } = new List<char[]>();

        public override char[] Rent(int minimumLength)
        {
            return new char[minimumLength];
        }

        public override void Return(char[] buffer, bool clearArray = false)
        {
            Returned.Add(buffer);
        }
    }

    private class RentMoreArrayPool : ArrayPool<char>
    {
        public IList<char[]> Returned { get; } = new List<char[]>();

        public override char[] Rent(int minimumLength)
        {
            return new char[2 * minimumLength];
        }

        public override void Return(char[] buffer, bool clearArray = false)
        {
            Returned.Add(buffer);
        }
    }

    [Fact]
    public void WriteUtf8_WritesBytesDirectlyToInnerWriter()
    {
        var stream = new MemoryStream();
        var inner = new HttpResponseStreamWriter(stream, System.Text.Encoding.UTF8);
        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        writer.WriteUtf8("<h1>Hello</h1>"u8);
        inner.Flush();

        Assert.Equal("<h1>Hello</h1>", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteUtf8Async_WritesBytesDirectlyToInnerWriter()
    {
        var stream = new MemoryStream();
        var inner = new HttpResponseStreamWriter(stream, System.Text.Encoding.UTF8);
        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        await writer.WriteUtf8Async("<h1>Hello</h1>"u8.ToArray());
        await inner.FlushAsync();

        Assert.Equal("<h1>Hello</h1>", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteUtf8Async_FlushesBufferedCharsFirst_MaintainsOrdering()
    {
        var stream = new MemoryStream();
        var inner = new HttpResponseStreamWriter(stream, System.Text.Encoding.UTF8);
        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        writer.Write("Hello ");
        await writer.WriteUtf8Async("<b>World</b>"u8.ToArray());
        await writer.FlushAsync();
        await inner.FlushAsync();

        var output = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("Hello <b>World</b>", output);
    }

    [Fact]
    public async Task WriteUtf8Async_MixedCharsAndBytes_InterleavedCorrectly()
    {
        var stream = new MemoryStream();
        var inner = new HttpResponseStreamWriter(stream, System.Text.Encoding.UTF8);
        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        writer.Write("<html>");
        await writer.WriteUtf8Async("<head>"u8.ToArray());
        writer.Write("<title>T</title>");
        await writer.WriteUtf8Async("</head>"u8.ToArray());
        writer.Write("<body></body></html>");
        await writer.FlushAsync();
        await inner.FlushAsync();

        var output = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("<html><head><title>T</title></head><body></body></html>", output);
    }

    [Fact]
    public async Task WriteUtf8Async_FallsBackToString_WhenInnerIsNotHttpResponseStreamWriter()
    {
        var inner = new StringWriter();
        var writer = new PagedBufferedTextWriter(new TestArrayPool(), inner);

        writer.Write("prefix ");
        await writer.WriteUtf8Async("<p>test</p>"u8.ToArray());
        await writer.FlushAsync();

        Assert.Equal("prefix <p>test</p>", inner.ToString());
    }
}
