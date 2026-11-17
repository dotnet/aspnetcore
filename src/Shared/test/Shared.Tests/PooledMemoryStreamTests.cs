// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.WebUtilities;
using Moq;

namespace Microsoft.AspNetCore.Shared.Tests;

public class PooledMemoryStreamTests
{
    [Fact]
    public void Constructor_DoesNotRentUntilCapacityIsNeeded()
    {
        var pool = new Mock<ArrayPool<byte>>(MockBehavior.Strict);
        using var stream = new PooledMemoryStream(pool.Object, 16);

        Assert.Equal(0, stream.Length);
        Assert.Equal(0, stream.Position);
        Assert.True(stream.WrittenMemory.IsEmpty);
        pool.Verify(p => p.Rent(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void Constructor_ValidatesArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new PooledMemoryStream(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PooledMemoryStream(ArrayPool<byte>.Shared, -1));
    }

    [Fact]
    public void Write_RentsExactInitialSizeAndGrowsGeometricallyUpToMaximum()
    {
        var (pool, rentedSizes, returnedBuffers) = CreateExactSizePool();
        using var stream = new PooledMemoryStream(pool.Object, 10);

        stream.Write([1, 2, 3]);
        stream.WriteByte(4);
        stream.Write([5, 6, 7]);

        Assert.Equal([3, 6, 10], rentedSizes);
        Assert.Equal(2, returnedBuffers.Count);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], stream.WrittenMemory.ToArray());
    }

    [Fact]
    public void Write_WhenRequiredSizeIsMoreThanDouble_GrowsToExactRequiredSize()
    {
        var (pool, rentedSizes, _) = CreateExactSizePool();
        using var stream = new PooledMemoryStream(pool.Object, 20);

        stream.Write([1, 2]);
        stream.SetLength(7);

        Assert.Equal([2, 7], rentedSizes);
        Assert.Equal([1, 2, 0, 0, 0, 0, 0], stream.WrittenMemory.ToArray());
    }

    [Fact]
    public void Growth_PreservesWrittenDataAndCurrentPosition()
    {
        var (pool, _, _) = CreateExactSizePool();
        using var stream = new PooledMemoryStream(pool.Object, 16);

        stream.Write([1, 2, 3, 4]);
        stream.Position = 2;
        stream.Write([9, 8, 7, 6]);

        Assert.Equal(6, stream.Position);
        Assert.Equal(6, stream.Length);
        Assert.Equal([1, 2, 9, 8, 7, 6], stream.WrittenMemory.ToArray());
    }

    [Fact]
    public void Read_SupportsArrayAndSpanOverloads()
    {
        using var stream = CreateStreamWithData([1, 2, 3, 4, 5]);
        stream.Position = 0;
        var array = new byte[4];

        var arrayRead = stream.Read(array, 1, 2);
        Span<byte> span = stackalloc byte[3];
        var spanRead = stream.Read(span);

        Assert.Equal(2, arrayRead);
        Assert.Equal([0, 1, 2, 0], array);
        Assert.Equal(3, spanRead);
        Assert.Equal([3, 4, 5], span.ToArray());
        Assert.Equal(5, stream.Position);
        Assert.Equal(0, stream.Read(array, 0, array.Length));
    }

    [Fact]
    public async Task ReadAsync_SupportsArrayAndMemoryOverloads()
    {
        await using var stream = CreateStreamWithData([1, 2, 3, 4, 5]);
        stream.Position = 0;
        var array = new byte[5];

        var arrayRead = await stream.ReadAsync(array, 1, 2, CancellationToken.None);
        var memoryRead = await stream.ReadAsync(array.AsMemory(3, 2));

        Assert.Equal(2, arrayRead);
        Assert.Equal(2, memoryRead);
        Assert.Equal([0, 1, 2, 3, 4], array);
        Assert.Equal(4, stream.Position);
    }

    [Fact]
    public async Task WriteAsync_SupportsArrayAndMemoryOverloads()
    {
        await using var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 8);
        var data = new byte[] { 0, 1, 2, 3, 4 };

        await stream.WriteAsync(data, 1, 2, CancellationToken.None);
        await stream.WriteAsync(data.AsMemory(3, 2));

        Assert.Equal([1, 2, 3, 4], stream.WrittenMemory.ToArray());
        Assert.Equal(4, stream.Position);
    }

    [Theory]
    [InlineData(SeekOrigin.Begin, 2, 2)]
    [InlineData(SeekOrigin.Current, -1, 1)]
    [InlineData(SeekOrigin.End, -2, 3)]
    public void Seek_ValidOffset_UpdatesPosition(SeekOrigin origin, long offset, long expected)
    {
        using var stream = CreateStreamWithData([1, 2, 3, 4, 5]);
        stream.Position = 2;

        var position = stream.Seek(offset, origin);

        Assert.Equal(expected, position);
        Assert.Equal(expected, stream.Position);
        Assert.Equal(5, stream.Length);
    }

    [Fact]
    public void Position_CanMovePastEndWithoutChangingLength()
    {
        using var stream = CreateStreamWithData([1, 2, 3]);

        stream.Position = 10;

        Assert.Equal(10, stream.Position);
        Assert.Equal(3, stream.Length);
        Assert.Equal(-1, stream.ReadByte());
        Assert.Equal(10, stream.Position);
    }

    [Fact]
    public void PositionAndSeek_ValidateBoundaries()
    {
        using var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 8);

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = (long)int.MaxValue + 1);
        Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Seek(0, (SeekOrigin)int.MaxValue));
    }

    [Fact]
    public void SparseWrite_ZeroFillsGap()
    {
        using var stream = CreateStreamWithData([1, 2]);
        stream.Position = 5;

        stream.WriteByte(9);

        Assert.Equal(6, stream.Length);
        Assert.Equal([1, 2, 0, 0, 0, 9], stream.WrittenMemory.ToArray());
    }

    [Fact]
    public void SetLength_GrowsWithZerosAndShrinksPosition()
    {
        using var stream = CreateStreamWithData([1, 2, 3]);

        stream.SetLength(6);
        Assert.Equal([1, 2, 3, 0, 0, 0], stream.WrittenMemory.ToArray());
        Assert.Equal(3, stream.Position);

        stream.Position = 5;
        stream.SetLength(2);

        Assert.Equal(2, stream.Length);
        Assert.Equal(2, stream.Position);
        Assert.Equal([1, 2], stream.WrittenMemory.ToArray());
    }

    [Fact]
    public void SetLength_ValidatesBoundaries()
    {
        using var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 8);

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(9));
    }

    [Fact]
    public void Write_ExceedingMaximumCapacityThrowsWithoutChangingContent()
    {
        var (pool, rentedSizes, _) = CreateExactSizePool();
        using var stream = new PooledMemoryStream(pool.Object, 4);
        stream.Write([1, 2, 3, 4]);

        var exception = Assert.Throws<IOException>(() => stream.WriteByte(5));

        Assert.Equal("Memory threshold exceeded.", exception.Message);
        Assert.Equal(4, stream.Position);
        Assert.Equal(4, stream.Length);
        Assert.Equal([1, 2, 3, 4], stream.WrittenMemory.ToArray());
        Assert.Equal([4], rentedSizes);
    }

    [Fact]
    public void Write_WhenPositionPlusCountOverflowsThrowsWithoutRenting()
    {
        var pool = new Mock<ArrayPool<byte>>(MockBehavior.Strict);
        using var stream = new PooledMemoryStream(pool.Object, int.MaxValue);
        stream.Position = int.MaxValue;

        Assert.Throws<OverflowException>(() => stream.WriteByte(1));
        Assert.Equal(int.MaxValue, stream.Position);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task ZeroByteOperations_DoNotRentOrChangeState()
    {
        var pool = new Mock<ArrayPool<byte>>(MockBehavior.Strict);
        await using var stream = new PooledMemoryStream(pool.Object, 8);
        var buffer = Array.Empty<byte>();

        stream.Write(buffer, 0, 0);
        Assert.Equal(0, stream.Read(buffer, 0, 0));
        await stream.WriteAsync(buffer, CancellationToken.None);
        Assert.Equal(0, await stream.ReadAsync(buffer, CancellationToken.None));
        stream.SetLength(0);
        stream.Flush();
        await stream.FlushAsync();

        Assert.Equal(0, stream.Position);
        Assert.Equal(0, stream.Length);
        Assert.True(stream.WrittenMemory.IsEmpty);
        pool.Verify(p => p.Rent(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AsyncOperations_WithCanceledTokenThrowWithoutChangingState()
    {
        await using var stream = CreateStreamWithData([1, 2, 3]);
        stream.Position = 1;
        var canceledToken = new CancellationToken(canceled: true);

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stream.ReadAsync(new byte[1], canceledToken));
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stream.WriteAsync(new byte[] { 9 }, canceledToken));

        Assert.Equal(1, stream.Position);
        Assert.Equal([1, 2, 3], stream.WrittenMemory.ToArray());
    }

    [Fact]
    public void WrittenMemory_ContainsOnlyLogicalContentAndReflectsOverwrites()
    {
        using var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 16);
        stream.Write([1, 2, 3, 4]);
        stream.Position = 1;
        stream.Write([8, 9]);

        var writtenMemory = stream.WrittenMemory;

        Assert.Equal(4, writtenMemory.Length);
        Assert.Equal([1, 8, 9, 4], writtenMemory.ToArray());
    }

    [Fact]
    public void WrittenMemory_CanBeCopiedWithoutChangingPositionForSpill()
    {
        using var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 16);
        stream.Write([1, 2]);
        stream.Write([3, 4, 5]);
        stream.Position = 2;
        using var destination = new MemoryStream();

        destination.Write(stream.WrittenMemory.Span);

        Assert.Equal([1, 2, 3, 4, 5], destination.ToArray());
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void Dispose_ReturnsEveryRentedBufferExactlyOnce()
    {
        var (pool, _, returnedBuffers) = CreateExactSizePool();
        var stream = new PooledMemoryStream(pool.Object, 8);
        stream.Write([1, 2]);
        stream.Write([3, 4, 5]);

        stream.Dispose();
        stream.Dispose();

        Assert.Equal(2, returnedBuffers.Count);
        Assert.Equal(2, returnedBuffers.Distinct().Count());
        pool.Verify(p => p.Return(It.IsAny<byte[]>(), false), Times.Exactly(2));
    }

    [Fact]
    public async Task DisposeAsync_ReturnsBufferAndIsIdempotentWithDispose()
    {
        var (pool, _, returnedBuffers) = CreateExactSizePool();
        var stream = new PooledMemoryStream(pool.Object, 8);
        stream.Write([1, 2, 3]);

        await stream.DisposeAsync();
        await stream.DisposeAsync();
        stream.Dispose();

        Assert.Single(returnedBuffers);
        pool.Verify(p => p.Return(It.IsAny<byte[]>(), false), Times.Once);
    }

    [Fact]
    public async Task DisposedStream_ReportsClosedAndOperationsThrow()
    {
        var stream = CreateStreamWithData([1, 2, 3]);
        await stream.DisposeAsync();

        Assert.False(stream.CanRead);
        Assert.False(stream.CanSeek);
        Assert.False(stream.CanWrite);
        Assert.Throws<ObjectDisposedException>(() => stream.Flush());
        Assert.Throws<ObjectDisposedException>(() => _ = stream.Length);
        Assert.Throws<ObjectDisposedException>(() => _ = stream.Position);
        Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
        Assert.Throws<ObjectDisposedException>(() => _ = stream.WrittenMemory);
        Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1], 0, 1));
        Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
        Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<ObjectDisposedException>(() => stream.SetLength(0));
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await stream.ReadAsync(new byte[1], CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await stream.WriteAsync(new byte[1], CancellationToken.None));
    }

    private static PooledMemoryStream CreateStreamWithData(byte[] data)
    {
        var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 32);
        stream.Write(data);
        return stream;
    }

    private static (Mock<ArrayPool<byte>> Pool, List<int> RentedSizes, List<byte[]> ReturnedBuffers) CreateExactSizePool()
    {
        var rentedSizes = new List<int>();
        var returnedBuffers = new List<byte[]>();
        var pool = new Mock<ArrayPool<byte>>();
        pool.Setup(p => p.Rent(It.IsAny<int>()))
            .Returns((int size) =>
            {
                rentedSizes.Add(size);
                return new byte[size];
            });
        pool.Setup(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()))
            .Callback((byte[] buffer, bool _) => returnedBuffers.Add(buffer));

        return (pool, rentedSizes, returnedBuffers);
    }
}
