// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.WebUtilities;

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
        using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
        var input = Encoding.UTF8.GetBytes("Hello world");

        // Act
        bufferingStream.Write(input, 0, input.Length);

        // Assert
        Assert.Equal(input.Length, bufferingStream.Length);

        // We should have written content to memory
        var pagedByteBuffer = bufferingStream.PagedByteBuffer;
        Assert.Equal(input, ReadBufferedContent(pagedByteBuffer));

        // No files should not have been created.
        Assert.Null(bufferingStream.FileStream);
    }

    [Fact]
    public void Write_BeforeMemoryThresholdIsReached_WritesToMemory()
    {
        // Arrange
        var input = new byte[] { 1, 2, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        bufferingStream.Write(input, 0, 2);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        Assert.Equal(input.Length, bufferingStream.Length);

        // File should have been created.
        Assert.Null(fileStream);

        // No content should be in the memory stream
        Assert.Equal(2, pageBuffer.Length);
        Assert.Equal(input, ReadBufferedContent(pageBuffer));
    }

    [Fact]
    public void Write_BuffersContentToDisk_WhenMemoryThresholdIsReached()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);
        bufferingStream.Write(input, 0, 2);

        // Act
        bufferingStream.Write(input, 2, 1);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.NotNull(fileStream);
        var fileBytes = ReadFileContent(fileStream!);
        Assert.Equal(input, fileBytes);

        // No content should be in the memory stream
        Assert.Equal(0, pageBuffer.Length);
    }

    [Fact]
    public void Write_BuffersContentToDisk_WhenWriteWillOverflowMemoryThreshold()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        bufferingStream.Write(input, 0, input.Length);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.NotNull(fileStream);
        var fileBytes = ReadFileContent(fileStream!);
        Assert.Equal(input, fileBytes);

        // No content should be in the memory stream
        Assert.Equal(0, pageBuffer.Length);
    }

    [Fact]
    public void Write_AfterMemoryThresholdIsReached_BuffersToMemory()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 4, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        bufferingStream.Write(input, 0, 5);
        bufferingStream.Write(input, 5, 2);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.NotNull(fileStream);
        var fileBytes = ReadFileContent(fileStream!);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, }, fileBytes);

        Assert.Equal(new byte[] { 6, 7 }, ReadBufferedContent(pageBuffer));
    }

    [Fact]
    public async Task WriteAsync_BuffersContentToMemory()
    {
        // Arrange
        using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
        var input = Encoding.UTF8.GetBytes("Hello world");

        // Act
        await bufferingStream.WriteAsync(input, 0, input.Length);

        // Assert
        // We should have written content to memory
        var pagedByteBuffer = bufferingStream.PagedByteBuffer;
        Assert.Equal(input, ReadBufferedContent(pagedByteBuffer));

        // No files should not have been created.
        Assert.Null(bufferingStream.FileStream);
    }

    [Fact]
    public async Task WriteAsync_BeforeMemoryThresholdIsReached_WritesToMemory()
    {
        // Arrange
        var input = new byte[] { 1, 2, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        await bufferingStream.WriteAsync(input, 0, 2);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.Null(fileStream);

        // No content should be in the memory stream
        Assert.Equal(2, pageBuffer.Length);
        Assert.Equal(input, ReadBufferedContent(pageBuffer));
    }

    [Fact]
    public async Task WriteAsync_BuffersContentToDisk_WhenMemoryThresholdIsReached()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);
        bufferingStream.Write(input, 0, 2);

        // Act
        await bufferingStream.WriteAsync(input, 2, 1);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.NotNull(fileStream);
        var fileBytes = ReadFileContent(fileStream!);
        Assert.Equal(input, fileBytes);

        // No content should be in the memory stream
        Assert.Equal(0, pageBuffer.Length);
    }

    [Fact]
    public async Task WriteAsync_BuffersContentToDisk_WhenWriteWillOverflowMemoryThreshold()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        await bufferingStream.WriteAsync(input, 0, input.Length);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.NotNull(fileStream);
        var fileBytes = ReadFileContent(fileStream!);
        Assert.Equal(input, fileBytes);

        // No content should be in the memory stream
        Assert.Equal(0, pageBuffer.Length);
    }

    [Fact]
    public async Task WriteAsync_AfterMemoryThresholdIsReached_BuffersToMemory()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 4, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        await bufferingStream.WriteAsync(input, 0, 5);
        await bufferingStream.WriteAsync(input, 5, 2);

        // Assert
        var pageBuffer = bufferingStream.PagedByteBuffer;
        var fileStream = bufferingStream.FileStream;

        // File should have been created.
        Assert.NotNull(fileStream);
        var fileBytes = ReadFileContent(fileStream!);

        Assert.Equal(input.Length, bufferingStream.Length);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, }, fileBytes);
        Assert.Equal(new byte[] { 6, 7 }, ReadBufferedContent(pageBuffer));
    }

    [Fact]
    public void Write_Throws_IfSingleWriteExceedsBufferLimit()
    {
        // Arrange
        var input = new byte[20];
        var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        var exception = Assert.Throws<IOException>(() => bufferingStream.Write(input, 0, input.Length));
        Assert.Equal("Buffer limit exceeded.", exception.Message);

        Assert.True(bufferingStream.Disposed);
    }

    [Fact]
    public void Write_Throws_IfWriteCumulativeWritesExceedsBuffersLimit()
    {
        // Arrange
        var input = new byte[6];
        var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        bufferingStream.Write(input, 0, input.Length);
        var exception = Assert.Throws<IOException>(() => bufferingStream.Write(input, 0, input.Length));
        Assert.Equal("Buffer limit exceeded.", exception.Message);

        // Verify we return the buffer.
        Assert.True(bufferingStream.Disposed);
    }

    [Fact]
    public void Write_DoesNotThrow_IfBufferLimitIsReached()
    {
        // Arrange
        var input = new byte[5];
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        bufferingStream.Write(input, 0, input.Length);
        bufferingStream.Write(input, 0, input.Length); // Should get to exactly the buffer limit, which is fine

        // If we got here, the test succeeded.
    }

    [Fact]
    public async Task WriteAsync_Throws_IfSingleWriteExceedsBufferLimit()
    {
        // Arrange
        var input = new byte[20];
        var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        var exception = await Assert.ThrowsAsync<IOException>(() => bufferingStream.WriteAsync(input, 0, input.Length));
        Assert.Equal("Buffer limit exceeded.", exception.Message);

        Assert.True(bufferingStream.Disposed);
    }

    [Fact]
    public async Task WriteAsync_Throws_IfWriteCumulativeWritesExceedsBuffersLimit()
    {
        // Arrange
        var input = new byte[6];
        var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        await bufferingStream.WriteAsync(input, 0, input.Length);
        var exception = await Assert.ThrowsAsync<IOException>(() => bufferingStream.WriteAsync(input, 0, input.Length));
        Assert.Equal("Buffer limit exceeded.", exception.Message);

        // Verify we return the buffer.
        Assert.True(bufferingStream.Disposed);
    }

    [Fact]
    public async Task WriteAsync_DoesNotThrow_IfBufferLimitIsReached()
    {
        // Arrange
        var input = new byte[5];
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

        // Act
        await bufferingStream.WriteAsync(input, 0, input.Length);
        await bufferingStream.WriteAsync(input, 0, input.Length); // Should get to exactly the buffer limit, which is fine

        // If we got here, the test succeeded.
    }

    [Fact]
    public async Task DrainBufferAsync_CopiesContentFromMemoryStream()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, 4, 5 };
        using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
        bufferingStream.Write(input, 0, input.Length);
        var memoryStream = new MemoryStream();

        // Act
        await bufferingStream.DrainBufferAsync(memoryStream, default);

        // Assert
        Assert.Equal(input, memoryStream.ToArray());
        Assert.Equal(0, bufferingStream.Length);
    }

    [Fact]
    public async Task DrainBufferAsync_WithContentInDisk_CopiesContentFromMemoryStream()
    {
        // Arrange
        var input = Enumerable.Repeat((byte)0xca, 30).ToArray();
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 21, tempFileDirectoryAccessor: () => TempDirectory);
        bufferingStream.Write(input, 0, input.Length);
        var memoryStream = new MemoryStream();

        // Act
        await bufferingStream.DrainBufferAsync(memoryStream, default);

        // Assert
        Assert.Equal(input, memoryStream.ToArray());
        Assert.Equal(0, bufferingStream.Length);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "UnixFileMode is not supported on Windows.")]
    public void Write_BufferingContentToDisk_CreatesFileWithUserOnlyUnixFileMode()
    {
        // Arrange
        var input = new byte[] { 1, 2, 3, };
        using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);
        bufferingStream.Write(input, 0, 2);

        // Act
        bufferingStream.Write(input, 2, 1);

        // Assert
        Assert.NotNull(bufferingStream.FileStream);
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(bufferingStream.FileStream.SafeFileHandle));
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
        var fs = new FileStream(fileStream.Name, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite);
        using var memoryStream = new MemoryStream();
        fs.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    private static byte[] ReadBufferedContent(PagedByteBuffer buffer)
    {
        using var memoryStream = new MemoryStream();
        buffer.MoveTo(memoryStream);

        return memoryStream.ToArray();
    }
}
