// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Internal;

public class PrefixedReadStreamTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Read_ReturnsFirstReadThenRestOfBufferThenInner(bool async)
    {
        var stream = new PrefixedReadStream([1, 2, 3, 4, 5, 0, 0], firstReadLength: 2, length: 5, new MemoryStream([6, 7]));

        Assert.Equal([[1, 2], [3, 4, 5], [6, 7]], await ReadAllAsync(stream, readSize: 100, async));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Read_LimitsReadsToDestinationLength(bool async)
    {
        var stream = new PrefixedReadStream([1, 2, 3, 4, 5], firstReadLength: 3, length: 5, new MemoryStream([6, 7, 8]));

        Assert.Equal([[1, 2], [3], [4, 5], [6, 7], [8]], await ReadAllAsync(stream, readSize: 2, async));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    public async Task Read_WithoutSecondSegment_ReturnsBufferAsOneRead(int firstReadLength, bool async)
    {
        var stream = new PrefixedReadStream([1, 2, 3, 4], firstReadLength, length: 4, new MemoryStream([5]));

        Assert.Equal([[1, 2, 3, 4], [5]], await ReadAllAsync(stream, readSize: 100, async));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Read_EmptyBuffer_ReadsInner(bool async)
    {
        var stream = new PrefixedReadStream([], firstReadLength: 0, length: 0, new MemoryStream([1, 2]));

        Assert.Equal([[1, 2]], await ReadAllAsync(stream, readSize: 100, async));
    }

    [Fact]
    public void Read_ByteArrayOverload_HonorsOffsetAndCount()
    {
        var stream = new PrefixedReadStream([1, 2, 3], firstReadLength: 3, length: 3, new MemoryStream());
        var destination = new byte[5];

        Assert.Equal(2, stream.Read(destination, 1, 2));
        Assert.Equal([0, 1, 2, 0, 0], destination);
    }

    [Fact]
    public async Task ReadAsync_PassesCancellationTokenToInner()
    {
        var stream = new PrefixedReadStream([1], firstReadLength: 1, length: 1, new MemoryStream([2]));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Equal(1, await stream.ReadAsync(new byte[10], cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await stream.ReadAsync(new byte[10], cts.Token));
    }

    [Fact]
    public void Constructor_ValidatesArguments()
    {
        var inner = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => new PrefixedReadStream(null!, 0, 0, inner));
        Assert.Throws<ArgumentNullException>(() => new PrefixedReadStream([], 0, 0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PrefixedReadStream([1], -1, 1, inner));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PrefixedReadStream([1, 2], 2, 1, inner));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PrefixedReadStream([1], 0, 2, inner));
    }

    [Fact]
    public void Stream_IsReadOnlyAndNotSeekable()
    {
        var stream = new PrefixedReadStream([], 0, 0, new MemoryStream());

        Assert.True(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.False(stream.CanWrite);
        Assert.Throws<NotSupportedException>(() => stream.Length);
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
    }

    private static async Task<List<byte[]>> ReadAllAsync(Stream stream, int readSize, bool async)
    {
        var reads = new List<byte[]>();
        var buffer = new byte[readSize];
        while (true)
        {
            var read = async ? await stream.ReadAsync(buffer.AsMemory()) : stream.Read(buffer.AsSpan());
            if (read == 0)
            {
                return reads;
            }

            reads.Add(buffer.AsSpan(0, read).ToArray());
        }
    }
}
