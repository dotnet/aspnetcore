// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Moq;

namespace Microsoft.AspNetCore.WebUtilities;

public class PagedByteBufferTest
{
    [Fact]
    public void Add_CreatesNewPage()
    {
        // Arrange
        var input = Encoding.UTF8.GetBytes("Hello world");
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);

        // Act
        buffer.Add(input, 0, input.Length);

        // Assert
        Assert.Single(buffer.Pages);
        Assert.Equal(input.Length, buffer.Length);
        Assert.Equal(input, ReadBufferedContent(buffer));
    }

    [Fact]
    public void Add_AppendsToExistingPage()
    {
        // Arrange
        var input1 = Encoding.UTF8.GetBytes("Hello");
        var input2 = Encoding.UTF8.GetBytes("world");
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
        buffer.Add(input1, 0, input1.Length);

        // Act
        buffer.Add(input2, 0, input2.Length);

        // Assert
        Assert.Single(buffer.Pages);
        Assert.Equal(10, buffer.Length);
        Assert.Equal(Enumerable.Concat(input1, input2).ToArray(), ReadBufferedContent(buffer));
    }

    [Fact]
    public void Add_WithOffsets()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, 4, 5 };
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);

        // Act
        buffer.Add(input, 1, 3);

        // Assert
        Assert.Single(buffer.Pages);
        Assert.Equal(3, buffer.Length);
        Assert.Equal(new byte[] { 2, 3, 4 }, ReadBufferedContent(buffer));
    }

    [Fact]
    public void Add_FillsUpBuffer()
    {
        // Arrange
        var input1 = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize - 1).ToArray();
        var input2 = new byte[] { 0xca };
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
        buffer.Add(input1, 0, input1.Length);

        // Act
        buffer.Add(input2, 0, 1);

        // Assert
        Assert.Single(buffer.Pages);
        Assert.Equal(PagedByteBuffer.PageSize, buffer.Length);
        Assert.Equal(Enumerable.Concat(input1, input2).ToArray(), ReadBufferedContent(buffer));
    }

    [Fact]
    public void Add_AppendsToMultiplePages()
    {
        // Arrange
        var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize + 10).ToArray();
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);

        // Act
        buffer.Add(input, 0, input.Length);

        // Assert
        Assert.Equal(2, buffer.Pages.Count);
        Assert.Equal(PagedByteBuffer.PageSize + 10, buffer.Length);
        Assert.Equal(input.ToArray(), ReadBufferedContent(buffer));
    }

    [Fact]
    public void MoveTo_CopiesContentToStream()
    {
        // Arrange
        var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
        buffer.Add(input, 0, input.Length);
        var stream = new MemoryStream();

        // Act
        buffer.MoveTo(stream);

        // Assert
        Assert.Equal(input, stream.ToArray());

        // Verify moving new content works.
        var newInput = Enumerable.Repeat((byte)0xcb, PagedByteBuffer.PageSize * 2 + 13).ToArray();
        buffer.Add(newInput, 0, newInput.Length);

        stream.SetLength(0);
        buffer.MoveTo(stream);

        Assert.Equal(newInput, stream.ToArray());
    }

    [Fact]
    public async Task MoveToAsync_CopiesContentToStream()
    {
        // Arrange
        var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
        buffer.Add(input, 0, input.Length);
        var stream = new MemoryStream();

        // Act
        await buffer.MoveToAsync(stream, default);

        // Assert
        Assert.Equal(input, stream.ToArray());

        // Verify adding and moving new content works.
        var newInput = Enumerable.Repeat((byte)0xcb, PagedByteBuffer.PageSize * 2 + 13).ToArray();
        buffer.Add(newInput, 0, newInput.Length);
        stream.SetLength(0);
        await buffer.MoveToAsync(stream, default);

        Assert.Equal(newInput, stream.ToArray());
    }

    [Fact]
    public async Task MoveToAsync_ClearsBuffers()
    {
        // Arrange
        var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
        using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
        buffer.Add(input, 0, input.Length);
        var stream = new MemoryStream();

        // Act
        await buffer.MoveToAsync(stream, default);

        // Assert
        Assert.Equal(input, stream.ToArray());

        // Verify copying it again works.
        Assert.Equal(0, buffer.Length);
        Assert.False(buffer.Disposed);
        Assert.Empty(buffer.Pages);
    }

    [Fact]
    public void MoveTo_WithClear_ReturnsBuffers()
    {
        // Arrange
        var input = new byte[] { 1, };
        var arrayPool = new Mock<ArrayPool<byte>>();
        var byteArray = new byte[PagedByteBuffer.PageSize];
        arrayPool.Setup(p => p.Rent(PagedByteBuffer.PageSize))
            .Returns(byteArray);
        arrayPool.Setup(p => p.Return(byteArray, false)).Verifiable();
        var memoryStream = new MemoryStream();

        using (var buffer = new PagedByteBuffer(arrayPool.Object))
        {
            // Act
            buffer.Add(input, 0, input.Length);
            buffer.MoveTo(memoryStream);

            // Assert
            Assert.Equal(input, memoryStream.ToArray());
        }

        arrayPool.Verify(p => p.Rent(It.IsAny<int>()), Times.Once());
        arrayPool.Verify(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()), Times.Once());
    }

    [Fact]
    public async Task MoveToAsync_ReturnsBuffers()
    {
        // Arrange
        var input = new byte[] { 1, };
        var arrayPool = new Mock<ArrayPool<byte>>();
        var byteArray = new byte[PagedByteBuffer.PageSize];
        arrayPool.Setup(p => p.Rent(PagedByteBuffer.PageSize))
            .Returns(byteArray);
        var memoryStream = new MemoryStream();

        using (var buffer = new PagedByteBuffer(arrayPool.Object))
        {
            // Act
            buffer.Add(input, 0, input.Length);
            await buffer.MoveToAsync(memoryStream, default);

            // Assert
            Assert.Equal(input, memoryStream.ToArray());
        }

        arrayPool.Verify(p => p.Rent(It.IsAny<int>()), Times.Once());
        arrayPool.Verify(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()), Times.Once());
    }

    [Fact]
    public void Dispose_ReturnsBuffers_ExactlyOnce()
    {
        // Arrange
        var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
        var arrayPool = new Mock<ArrayPool<byte>>();
        arrayPool.Setup(p => p.Rent(PagedByteBuffer.PageSize))
            .Returns(new byte[PagedByteBuffer.PageSize]);

        var buffer = new PagedByteBuffer(arrayPool.Object);

        // Act
        buffer.Add(input, 0, input.Length);
        buffer.Dispose();
        buffer.Dispose();

        arrayPool.Verify(p => p.Rent(It.IsAny<int>()), Times.Exactly(4));
        arrayPool.Verify(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()), Times.Exactly(4));
    }

    private static byte[] ReadBufferedContent(PagedByteBuffer buffer)
    {
        using var stream = new MemoryStream();
        buffer.MoveTo(stream);
        return stream.ToArray();
    }
}
