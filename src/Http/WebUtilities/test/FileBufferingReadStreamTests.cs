// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.WebUtilities;

public class FileBufferingReadStreamTests
{
    private Stream MakeStream(int size)
    {
        // TODO: Fill with random data? Make readonly?
        return new MemoryStream(new byte[size]);
    }

    [Fact]
    public void FileBufferingReadStream_Properties_ExpectedValues()
    {
        using var inner = MakeStream(1024 * 2);
        System.IO.Stream bufferSteam;
        {
            using var stream = new FileBufferingReadStream(inner, 1024, null, Directory.GetCurrentDirectory());
            bufferSteam = stream;
            Assert.True(stream.CanRead);
            Assert.True(stream.CanSeek);
            Assert.False(stream.CanWrite);
            Assert.Equal(0, stream.Length); // Nothing buffered yet
            Assert.Equal(0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);
        }
        Assert.False(bufferSteam.CanRead);  // Buffered Stream now disposed
        Assert.False(bufferSteam.CanSeek);
        Assert.True(inner.CanRead);         // Inner Stream not disposed
        Assert.True(inner.CanSeek);
    }

    [Fact]
    public void FileBufferingReadStream_Sync0ByteReadUnderThreshold_DoesntCreateFile()
    {
        var inner = MakeStream(1024);
        using (var stream = new FileBufferingReadStream(inner, 1024 * 2, null, Directory.GetCurrentDirectory()))
        {
            var bytes = new byte[1000];
            var read0 = stream.Read(bytes, 0, 0);
            Assert.Equal(0, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read2 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1, read2);
            Assert.Equal(read0 + read1 + read2, stream.Length);
            Assert.Equal(read0 + read1 + read2, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read3 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(0, read3);
        }
    }

    [Fact]
    public void FileBufferingReadStream_SyncReadUnderThreshold_DoesntCreateFile()
    {
        var inner = MakeStream(1024 * 2);
        using (var stream = new FileBufferingReadStream(inner, 1024 * 3, null, Directory.GetCurrentDirectory()))
        {
            var bytes = new byte[1000];
            var read0 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read2 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1, read2);
            Assert.Equal(read0 + read1 + read2, stream.Length);
            Assert.Equal(read0 + read1 + read2, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read3 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(0, read3);
        }
    }

    [Fact]
    public void FileBufferingReadStream_SyncReadOverThreshold_CreatesFile()
    {
        var inner = MakeStream(1024 * 2);
        string tempFileName;
        using (var stream = new FileBufferingReadStream(inner, 1024, null, GetCurrentDirectory()))
        {
            var bytes = new byte[1000];
            var read0 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            tempFileName = stream.TempFileName!;
            Assert.True(File.Exists(tempFileName));

            var read2 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1, read2);
            Assert.Equal(read0 + read1 + read2, stream.Length);
            Assert.Equal(read0 + read1 + read2, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            Assert.True(File.Exists(tempFileName));

            var read3 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(0, read3);
        }

        Assert.False(File.Exists(tempFileName));
    }

    [Fact]
    public void FileBufferingReadStream_SyncReadWithInMemoryLimit_EnforcesLimit()
    {
        var inner = MakeStream(1024 * 2);
        using (var stream = new FileBufferingReadStream(inner, 1024, 900, Directory.GetCurrentDirectory()))
        {
            var bytes = new byte[500];
            var read0 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var exception = Assert.Throws<IOException>(() => stream.Read(bytes, 0, bytes.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);
            Assert.False(File.Exists(stream.TempFileName));
        }
    }

    [Fact]
    public void FileBufferingReadStream_SyncReadWithOnDiskLimit_EnforcesLimit()
    {
        var inner = MakeStream(1024 * 2);
        string tempFileName;
        using (var stream = new FileBufferingReadStream(inner, 512, 1024, GetCurrentDirectory()))
        {
            var bytes = new byte[500];
            var read0 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            tempFileName = stream.TempFileName!;
            Assert.True(File.Exists(tempFileName));

            var exception = Assert.Throws<IOException>(() => stream.Read(bytes, 0, bytes.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
        }

        Assert.False(File.Exists(tempFileName));
    }

    ///////////////////

    [Fact]
    public async Task FileBufferingReadStream_Async0ByteReadUnderThreshold_DoesntCreateFile()
    {
        var inner = MakeStream(1024);
        using (var stream = new FileBufferingReadStream(inner, 1024 * 2, null, Directory.GetCurrentDirectory()))
        {
            var bytes = new byte[1000];
            var read0 = await stream.ReadAsync(bytes, 0, 0);
            Assert.Equal(0, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read2 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1, read2);
            Assert.Equal(read0 + read1 + read2, stream.Length);
            Assert.Equal(read0 + read1 + read2, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read3 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(0, read3);
        }
    }

    [Fact]
    public async Task FileBufferingReadStream_AsyncReadUnderThreshold_DoesntCreateFile()
    {
        var inner = MakeStream(1024 * 2);
        using (var stream = new FileBufferingReadStream(inner, 1024 * 3, null, Directory.GetCurrentDirectory()))
        {
            var bytes = new byte[1000];
            var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read2 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1, read2);
            Assert.Equal(read0 + read1 + read2, stream.Length);
            Assert.Equal(read0 + read1 + read2, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read3 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(0, read3);
        }
    }

    [Fact]
    public async Task FileBufferingReadStream_AsyncReadOverThreshold_CreatesFile()
    {
        var inner = MakeStream(1024 * 2);
        string tempFileName;
        using (var stream = new FileBufferingReadStream(inner, 1024, null, GetCurrentDirectory()))
        {
            var bytes = new byte[1000];
            var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            tempFileName = stream.TempFileName!;
            Assert.True(File.Exists(tempFileName));

            var read2 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1, read2);
            Assert.Equal(read0 + read1 + read2, stream.Length);
            Assert.Equal(read0 + read1 + read2, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            Assert.True(File.Exists(tempFileName));

            var read3 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(0, read3);
        }

        Assert.False(File.Exists(tempFileName));
    }

    [Fact]
    public async Task FileBufferingReadStream_Async0ByteReadAfterBuffering_ReadsFromFile()
    {
        var inner = MakeStream(1024 * 2);
        string tempFileName;
        using (var stream = new FileBufferingReadStream(inner, 1024, null, GetCurrentDirectory()))
        {
            await stream.DrainAsync(default);
            stream.Position = 0;
            Assert.Equal(inner.Length, stream.Length);
            Assert.Equal(0, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            tempFileName = stream.TempFileName!;
            Assert.True(File.Exists(tempFileName));

            var bytes = new byte[1000];
            var read0 = await stream.ReadAsync(bytes, 0, 0);
            Assert.Equal(0, read0);
            Assert.Equal(read0, stream.Position);

            var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Position);

            var read2 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read2);
            Assert.Equal(read0 + read1 + read2, stream.Position);

            var read3 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(inner.Length - read0 - read1 - read2, read3);
            Assert.Equal(read0 + read1 + read2 + read3, stream.Length);
            Assert.Equal(read0 + read1 + read2 + read3, stream.Position);

            var read4 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(0, read4);
        }

        Assert.False(File.Exists(tempFileName));
    }

    [Fact]
    public async Task FileBufferingReadStream_AsyncReadWithInMemoryLimit_EnforcesLimit()
    {
        var inner = MakeStream(1024 * 2);
        using (var stream = new FileBufferingReadStream(inner, 1024, 900, Directory.GetCurrentDirectory()))
        {
            var bytes = new byte[500];
            var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var exception = await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(bytes, 0, bytes.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);
            Assert.False(File.Exists(stream.TempFileName));
        }
    }

    [Fact]
    public async Task FileBufferingReadStream_AsyncReadWithOnDiskLimit_EnforcesLimit()
    {
        var inner = MakeStream(1024 * 2);
        string tempFileName;
        using (var stream = new FileBufferingReadStream(inner, 512, 1024, GetCurrentDirectory()))
        {
            var bytes = new byte[500];
            var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.True(stream.InMemory);
            Assert.Null(stream.TempFileName);

            var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read1);
            Assert.Equal(read0 + read1, stream.Length);
            Assert.Equal(read0 + read1, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
            tempFileName = stream.TempFileName!;
            Assert.True(File.Exists(tempFileName));

            var exception = await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(bytes, 0, bytes.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);
        }

        Assert.False(File.Exists(tempFileName));
    }

    [Fact]
    public void FileBufferingReadStream_UsingMemoryStream_RentsAndReturnsRentedBuffer_WhenCopyingFromMemoryStreamDuringRead()
    {
        var inner = MakeStream(1024 * 1024 + 25);
        string tempFileName;
        var arrayPool = new Mock<ArrayPool<byte>>();
        arrayPool.Setup(p => p.Rent(It.IsAny<int>()))
            .Returns((int m) => ArrayPool<byte>.Shared.Rent(m));
        arrayPool.Setup(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()))
            .Callback((byte[] bytes, bool clear) => ArrayPool<byte>.Shared.Return(bytes, clear));

        using (var stream = new FileBufferingReadStream(inner, 1024 * 1024 + 1, 2 * 1024 * 1024, GetCurrentDirectory(), arrayPool.Object))
        {
            arrayPool.Verify(v => v.Rent(It.IsAny<int>()), Times.Never());

            stream.Read(new byte[1024 * 1024]);
            Assert.False(File.Exists(stream.TempFileName), "tempFile should not be created as yet");

            stream.Read(new byte[4]);
            Assert.True(File.Exists(stream.TempFileName), "tempFile should be created");
            tempFileName = stream.TempFileName!;

            arrayPool.Verify(v => v.Rent(It.IsAny<int>()), Times.Once());
            arrayPool.Verify(v => v.Return(It.IsAny<byte[]>(), It.IsAny<bool>()), Times.Once());
        }

        Assert.False(File.Exists(tempFileName));
    }

    [Fact]
    public async Task FileBufferingReadStream_UsingMemoryStream_RentsAndReturnsRentedBuffer_WhenCopyingFromMemoryStreamDuringReadAsync()
    {
        var inner = MakeStream(1024 * 1024 + 25);
        string tempFileName;
        var arrayPool = new Mock<ArrayPool<byte>>();
        arrayPool.Setup(p => p.Rent(It.IsAny<int>()))
            .Returns((int m) => ArrayPool<byte>.Shared.Rent(m));
        arrayPool.Setup(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()))
            .Callback((byte[] bytes, bool clear) => ArrayPool<byte>.Shared.Return(bytes, clear));

        using (var stream = new FileBufferingReadStream(inner, 1024 * 1024 + 1, 2 * 1024 * 1024, GetCurrentDirectory(), arrayPool.Object))
        {
            arrayPool.Verify(v => v.Rent(It.IsAny<int>()), Times.Never());

            await stream.ReadAsync(new byte[1024 * 1024]);
            Assert.False(File.Exists(stream.TempFileName), "tempFile should not be created as yet");

            await stream.ReadAsync(new byte[4]);
            Assert.True(File.Exists(stream.TempFileName), "tempFile should be created");
            tempFileName = stream.TempFileName!;

            arrayPool.Verify(v => v.Rent(It.IsAny<int>()), Times.Once());
            arrayPool.Verify(v => v.Return(It.IsAny<byte[]>(), It.IsAny<bool>()), Times.Once());
        }

        Assert.False(File.Exists(tempFileName));
    }

    [Fact]
    public async Task CopyToAsyncWorks()
    {
        // 4K is the lower bound on buffer sizes
        var bufferSize = 4096;
        var mostExpectedWrites = 8;
        var data = Enumerable.Range(0, bufferSize * mostExpectedWrites).Select(b => (byte)b).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 1024 * 1024, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new NumberOfWritesMemoryStream();
        await stream.CopyToAsync(withoutBufferMs);

        var withBufferMs = new NumberOfWritesMemoryStream();
        stream.Position = 0;
        await stream.CopyToAsync(withBufferMs);

        Assert.Equal(data, withoutBufferMs.ToArray());
        Assert.Equal(mostExpectedWrites, withoutBufferMs.NumberOfWrites);
        Assert.Equal(data, withBufferMs.ToArray());
        Assert.InRange(withBufferMs.NumberOfWrites, 1, mostExpectedWrites);
    }

    [Fact]
    public async Task CopyToAsyncWorksWithFileThreshold()
    {
        // 4K is the lower bound on buffer sizes
        var bufferSize = 4096;
        var mostExpectedWrites = 8;
        var data = Enumerable.Reverse(Enumerable.Range(0, bufferSize * mostExpectedWrites).Select(b => (byte)b)).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 100, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new NumberOfWritesMemoryStream();
        await stream.CopyToAsync(withoutBufferMs);

        var withBufferMs = new NumberOfWritesMemoryStream();
        stream.Position = 0;
        await stream.CopyToAsync(withBufferMs);

        Assert.Equal(data, withoutBufferMs.ToArray());
        Assert.Equal(mostExpectedWrites, withoutBufferMs.NumberOfWrites);
        Assert.Equal(data, withBufferMs.ToArray());
        Assert.InRange(withBufferMs.NumberOfWrites, 1, mostExpectedWrites);
    }

    [Fact]
    public async Task ReadAsyncThenCopyToAsyncWorks()
    {
        var data = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 1024 * 1024, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new MemoryStream();
        var buffer = new byte[100];
        await stream.ReadAsync(buffer);
        await stream.CopyToAsync(withoutBufferMs);

        Assert.Equal(data.AsMemory(0, 100).ToArray(), buffer);
        Assert.Equal(data.AsMemory(100).ToArray(), withoutBufferMs.ToArray());
    }

    [Fact]
    public async Task ReadThenCopyToAsyncWorks()
    {
        var data = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 1024 * 1024, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new MemoryStream();
        var buffer = new byte[100];
        var read = stream.Read(buffer);
        await stream.CopyToAsync(withoutBufferMs);

        Assert.Equal(100, read);
        Assert.Equal(data.AsMemory(0, read).ToArray(), buffer);
        Assert.Equal(data.AsMemory(read).ToArray(), withoutBufferMs.ToArray());
    }

    [Fact]
    public async Task ReadThenSeekThenCopyToAsyncWorks()
    {
        var data = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 1024 * 1024, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new MemoryStream();
        var buffer = new byte[100];
        var read = stream.Read(buffer);
        stream.Position = 0;
        await stream.CopyToAsync(withoutBufferMs);

        Assert.Equal(100, read);
        Assert.Equal(data.AsMemory(0, read).ToArray(), buffer);
        Assert.Equal(data.ToArray(), withoutBufferMs.ToArray());
    }

    [Fact]
    public void PartialReadThenSeekReplaysBuffer()
    {
        var data = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 1024 * 1024, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new MemoryStream();
        var buffer = new byte[100];
        var read1 = stream.Read(buffer);
        stream.Position = 0;
        var buffer2 = new byte[200];
        var read2 = stream.Read(buffer2);
        Assert.Equal(100, read1);
        Assert.Equal(100, read2);
        Assert.Equal(data.AsMemory(0, read1).ToArray(), buffer);
        Assert.Equal(data.AsMemory(0, read2).ToArray(), buffer2.AsMemory(0, read2).ToArray());
    }

    [Fact]
    public async Task PartialReadAsyncThenSeekReplaysBuffer()
    {
        var data = Enumerable.Range(0, 1024).Select(b => (byte)b).ToArray();
        var inner = new MemoryStream(data);

        using var stream = new FileBufferingReadStream(inner, 1024 * 1024, bufferLimit: null, GetCurrentDirectory());

        var withoutBufferMs = new MemoryStream();
        var buffer = new byte[100];
        var read1 = await stream.ReadAsync(buffer);
        stream.Position = 0;
        var buffer2 = new byte[200];
        var read2 = await stream.ReadAsync(buffer2);
        Assert.Equal(100, read1);
        Assert.Equal(100, read2);
        Assert.Equal(data.AsMemory(0, read1).ToArray(), buffer);
        Assert.Equal(data.AsMemory(0, read2).ToArray(), buffer2.AsMemory(0, read2).ToArray());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "UnixFileMode is not supported on Windows.")]
    public void Read_BufferingContentToDisk_CreatesFileWithUserOnlyUnixFileMode()
    {
        var inner = MakeStream(1024 * 2);
        string tempFileName;
        using (var stream = new FileBufferingReadStream(inner, 1024, null, GetCurrentDirectory()))
        {
            var bytes = new byte[1024 * 2];
            var read0 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(bytes.Length, read0);
            Assert.Equal(read0, stream.Length);
            Assert.Equal(read0, stream.Position);
            Assert.False(stream.InMemory);
            Assert.NotNull(stream.TempFileName);

            var read1 = stream.Read(bytes, 0, bytes.Length);
            Assert.Equal(0, read1);

            tempFileName = stream.TempFileName!;
            Assert.True(File.Exists(tempFileName));
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(tempFileName));
        }

        Assert.False(File.Exists(tempFileName));
    }

    private static string GetCurrentDirectory()
    {
        return AppContext.BaseDirectory;
    }

    private class NumberOfWritesMemoryStream : MemoryStream
    {
        public int NumberOfWrites { get; set; }

        public override void Write(byte[] buffer, int offset, int count)
        {
            NumberOfWrites++;
            base.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            NumberOfWrites++;
            base.Write(source);
        }
    }
}
