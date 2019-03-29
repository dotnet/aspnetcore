// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities.Tests
{
    public class FileBufferingWriteStreamTests : IDisposable
    {
        private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "FileBufferingWriteTests", Path.GetRandomFileName());

        public FileBufferingWriteStreamTests()
        {
            Directory.CreateDirectory(TempDirectory);
        }

        [Fact]
        public void Write_BuffersContentToMemory()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            using var bufferingStream = new FileBufferingWriteStream(writeStream, tempFileDirectoryAccessor: () => TempDirectory);
            var input = Encoding.UTF8.GetBytes("Hello world");

            // Act
            bufferingStream.Write(input, 0, input.Length);

            // Assert
            // We should have written content to the MemoryStream
            var memoryStream = bufferingStream.MemoryStream;
            Assert.Equal(input, memoryStream.ToArray());

            // No files should not have been created.
            Assert.Null(bufferingStream.FileStream);

            // No content should have been written to the wrapping stream
            Assert.Equal(0, writeStream.Length);
        }

        [Fact]
        public void Write_BuffersContentToDisk_WhenMemoryThresholdIsReached()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            var input = new byte[] { 1, 2, 3, 4, 5 };
            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, input.Length);

            // Assert
            var memoryStream = bufferingStream.MemoryStream;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(input, fileBytes);

            // No content should be in the memory stream
            Assert.Equal(0, memoryStream.Length);

            // No content should have been written to the wrapping stream
            Assert.Equal(0, writeStream.Length);
        }

        [Fact]
        public void Write_SpoolsContentFromMemoryToDisk()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            var input = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 4, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, 3);
            bufferingStream.Write(input, 3, 2);
            bufferingStream.Write(input, 5, 2);

            // Assert
            var memoryStream = bufferingStream.MemoryStream;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, fileBytes);

            Assert.Equal(new byte[] { 6, 7 }, memoryStream.ToArray());

            // No content should have been written to the wrapping stream
            Assert.Equal(0, writeStream.Length);
        }

        [Fact]
        public async Task WriteAsync_CopiesBufferedContentsFromMemoryStream()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            using var bufferingStream = new FileBufferingWriteStream(writeStream, tempFileDirectoryAccessor: () => TempDirectory);
            var input = Encoding.UTF8.GetBytes("Hello world");

            // Act
            bufferingStream.Write(input, 0, 5);
            await bufferingStream.WriteAsync(input, 5, input.Length - 5);

            // Assert
            // We should have written all content to the output.
            Assert.Equal(input, writeStream.ToArray());
        }

        [Fact]
        public async Task WriteAsync_CopiesBufferedContentsFromFileStream()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 1, tempFileDirectoryAccessor: () => TempDirectory);
            var input = Encoding.UTF8.GetBytes("Hello world");

            // Act
            bufferingStream.Write(input, 0, 5);
            await bufferingStream.WriteAsync(input, 5, input.Length - 5);

            // Assert
            // We should have written all content to the output.
            Assert.Equal(0, bufferingStream.BufferedLength);
            Assert.Equal(input, writeStream.ToArray());
        }

        [Fact]
        public async Task WriteFollowedByWriteAsync()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            using var bufferingStream = new FileBufferingWriteStream(writeStream);
            var input = new byte[] { 7, 8, 9 };

            // Act
            bufferingStream.Write(input, 0, 1);
            await bufferingStream.WriteAsync(input, 1, 1);
            bufferingStream.Write(input, 2, 1);

            // Assert
            Assert.Equal(new byte[] { 7, 8 }, writeStream.ToArray());
            Assert.Equal(1, bufferingStream.BufferedLength);
            Assert.Equal(new byte[] { 9 }, bufferingStream.MemoryStream.ToArray());
        }

        [Fact]
        public void Dispose_FlushesBufferedContent()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            using var bufferingStream = new FileBufferingWriteStream(writeStream);
            var input = new byte[] { 1, 2, 34 };

            // Act
            bufferingStream.Write(input, 0, input.Length);
            bufferingStream.Dispose();

            // Assert
            Assert.Equal(input, writeStream.ToArray());
        }

        [Fact]
        public void Dispose_ReturnsBuffer()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            var arrayPool = new Mock<ArrayPool<byte>>();
            var bytes = new byte[10];
            arrayPool.Setup(p => p.Rent(It.IsAny<int>())).Returns(bytes);
            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 2, null, tempFileDirectoryAccessor: () => TempDirectory, bytePool: arrayPool.Object);

            // Act
            bufferingStream.Dispose();
            bufferingStream.Dispose(); // Double disposing shouldn't be a problem

            // Assert
            Assert.True(bufferingStream.Disposed);
            arrayPool.Verify(v => v.Return(bytes, false), Times.Once());
        }

        [Fact]
        public async Task DisposeAsync_FlushesBufferedContent()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            using var bufferingStream = new FileBufferingWriteStream(writeStream);
            var input = new byte[] { 1, 2, 34 };

            // Act
            bufferingStream.Write(input, 0, input.Length);
            await bufferingStream.DisposeAsync();

            // Assert
            Assert.Equal(input, writeStream.ToArray());
        }

        [Fact]
        public async Task DisposeAsync_ReturnsBuffer()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            var arrayPool = new Mock<ArrayPool<byte>>();
            var bytes = new byte[10];
            arrayPool.Setup(p => p.Rent(It.IsAny<int>())).Returns(bytes);
            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 2, null, tempFileDirectoryAccessor: () => TempDirectory, bytePool: arrayPool.Object);

            // Act
            await bufferingStream.DisposeAsync();
            await bufferingStream.DisposeAsync(); // Double disposing shouldn't be a problem

            // Assert
            arrayPool.Verify(v => v.Return(bytes, false), Times.Once());
        }

        [Fact]
        public void Write_Throws_IfBufferLimitIsReached()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            var input = new byte[6];
            var arrayPool = new Mock<ArrayPool<byte>>();
            arrayPool.Setup(p => p.Rent(It.IsAny<byte>())).Returns(new byte[10]);

            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory, bytePool: arrayPool.Object);

            // Act
            bufferingStream.Write(input, 0, input.Length);
            var exception = Assert.Throws<IOException>(() => bufferingStream.Write(input, 0, input.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);

            // Verify we return the buffer.
            arrayPool.Verify(v => v.Return(It.IsAny<byte[]>(), false), Times.Once());
        }

        [Fact]
        public void Write_CopiesContentOnFlush()
        {
            // Arrange
            using var writeStream = new MemoryStream();
            var input = new byte[6];
            var arrayPool = new Mock<ArrayPool<byte>>();
            arrayPool.Setup(p => p.Rent(It.IsAny<byte>())).Returns(new byte[10]);

            using var bufferingStream = new FileBufferingWriteStream(writeStream, memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory, bytePool: arrayPool.Object);

            // Act
            bufferingStream.Write(input, 0, input.Length);
            var exception = Assert.Throws<IOException>(() => bufferingStream.Write(input, 0, input.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);

            // Verify we return the buffer.
            arrayPool.Verify(v => v.Return(It.IsAny<byte[]>(), false), Times.Once());
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
            catch
            {
            }
        }

        private static byte[] ReadFileContent(FileStream fileStream)
        {
            fileStream.Position = 0;
            var count = fileStream.Length;
            var bytes = new ArraySegment<byte>(new byte[fileStream.Length]);
            while (count > 0)
            {
                var read = fileStream.Read(bytes.AsSpan());
                Assert.False(read == 0, "Should not EOF before we've read the file.");
                bytes = bytes.Slice(read);
                count -= read;
            }

            return bytes.Array;
        }
    }
}
