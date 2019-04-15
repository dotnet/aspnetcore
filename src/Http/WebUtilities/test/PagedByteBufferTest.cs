// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
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
        public void CopyTo_CopiesContentToStream()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
            using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
            buffer.Add(input, 0, input.Length);
            var stream = new MemoryStream();

            // Act
            buffer.CopyTo(stream, clearBuffers: false);

            // Assert
            Assert.Equal(input, stream.ToArray());

            // Verify copying it again works.
            stream.SetLength(0);
            buffer.CopyTo(stream, clearBuffers: false);

            Assert.Equal(input, stream.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_CopiesContentToStream()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
            using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
            buffer.Add(input, 0, input.Length);
            var stream = new MemoryStream();

            // Act
            await buffer.CopyToAsync(stream, clearBuffers: false, default);

            // Assert
            Assert.Equal(input, stream.ToArray());

            // Verify copying it again works.
            stream.SetLength(0);
            await buffer.CopyToAsync(stream, clearBuffers: false, default);

            Assert.Equal(input, stream.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_WithClear_ClearsBuffers()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xba, PagedByteBuffer.PageSize * 3 + 10).ToArray();
            using var buffer = new PagedByteBuffer(ArrayPool<byte>.Shared);
            buffer.Add(input, 0, input.Length);
            var stream = new MemoryStream();

            // Act
            await buffer.CopyToAsync(stream, clearBuffers: true, default);

            // Assert
            Assert.Equal(input, stream.ToArray());

            // Verify copying it again works.
            Assert.Equal(0, buffer.Length);
            Assert.False(buffer.Disposed);
            Assert.Empty(buffer.Pages);
        }

        [Fact]
        public void CopyTo_WithClear_ReturnsBuffers()
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
                buffer.CopyTo(memoryStream, clearBuffers: true);

                // Assert
                Assert.Equal(input, memoryStream.ToArray());
            }

            arrayPool.Verify(p => p.Rent(It.IsAny<int>()), Times.Once());
            arrayPool.Verify(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()), Times.Once());
        }

        [Fact]
        public async Task CopyToAsync_WithClear_ReturnsBuffers()
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
                await buffer.CopyToAsync(memoryStream, clearBuffers: true, default);

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
            buffer.CopyTo(stream, clearBuffers: false);

            return stream.ToArray();
        }
    }
}
