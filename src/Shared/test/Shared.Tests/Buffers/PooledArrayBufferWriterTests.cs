// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Shared.Tests.Buffers;

public class PooledArrayBufferWriterTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(1024)]
    public void Constructor_WithInitialCapacity_InitializesWithRequestedCapacity(int initialCapacity)
    {
        using var writer = new PooledArrayBufferWriter<byte>(initialCapacity);

        Assert.Equal(0, writer.WrittenCount);
        Assert.True(writer.Capacity >= initialCapacity && writer.Capacity > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidInitialCapacity_Throws(int invalidCapacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PooledArrayBufferWriter<byte>(invalidCapacity));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Advance_WithNegativeCount_Throws(int negativeCount)
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(negativeCount));
    }

    [Fact]
    public void Advance_BeyondCapacity_Throws()
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        Assert.Throws<InvalidOperationException>(() => writer.Advance(20));
    }

    [Fact]
    public void GetSpan_WithInvalidSizeHint_Throws()
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(-1));
    }

    [Fact]
    public void GetMemory_WithInvalidSizeHint_Throws()
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetMemory(-1));
    }

    [Fact]
    public void WrittenCount_ReturnsNumberOfBytesWritten_AsPerAdvanceCalls()
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        writer.Advance(2);
        Assert.Equal(2, writer.WrittenCount);
        writer.Advance(1);
        Assert.Equal(3, writer.WrittenCount);
        writer.Advance(5);
        Assert.Equal(8, writer.WrittenCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetSpan_WithSizeHint_ResizesBufferIfNeeded(int sizeHint)
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        var span = writer.GetSpan(sizeHint);
        Assert.True(span.Length >= sizeHint && span.Length > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetMemory_WithSizeHint_ResizesBufferIfNeeded(int sizeHint)
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        var memory = writer.GetMemory(sizeHint);
        Assert.True(memory.Length >= sizeHint && memory.Length > 0);
    }

    [Fact]
    public void Advance_WithZero_HasNoEffect()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        writer.Advance(10);
        var countBefore = writer.WrittenCount;
        writer.Advance(0);

        Assert.Equal(countBefore, writer.WrittenCount);
    }

    [Fact]
    public void WrittenSpan_ReturnsSpanOfWrittenData()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        var writtenSpan = writer.WrittenSpan;
        Assert.Equal(2, writtenSpan.Length);
        Assert.Equal(42, writtenSpan[0]);
        Assert.Equal(99, writtenSpan[1]);
    }

    [Fact]
    public void WrittenMemory_ReturnsMemoryOfWrittenData()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        var writtenMemory = writer.WrittenMemory;
        Assert.Equal(2, writtenMemory.Length);
        Assert.Equal(42, writtenMemory.Span[0]);
        Assert.Equal(99, writtenMemory.Span[1]);
    }

    [Fact]
    public void Clear_ResetsWrittenCountAndClearsData()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        var span = writer.GetSpan();
        span[0] = 42;
        writer.Advance(1);
        writer.Clear();

        Assert.Equal(0, writer.WrittenCount);
        Assert.Equal(0, writer.WrittenSpan.Length);
    }

    [Fact]
    public void Clear_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.Clear());
        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan());
        Assert.Throws<ObjectDisposedException>(() => writer.GetMemory());
        Assert.Throws<ObjectDisposedException>(() => writer.Advance(1));
        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenSpan);
        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenMemory);
        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenCount);
        Assert.Throws<ObjectDisposedException>(() => _ = writer.Capacity);
        Assert.Throws<ObjectDisposedException>(() => _ = writer.FreeCapacity);
    }

    [Fact]
    public void BufferPreservesContentOnGrowth_AndAllowsWritingMore()
    {
        var initialCapacity = 10;
        using var writer = new PooledArrayBufferWriter<byte>(initialCapacity);

        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        var contentBefore = writer.WrittenSpan.ToArray();

        // Force buffer growth
        span = writer.GetSpan(100);
        Assert.True(writer.Capacity > initialCapacity);

        var contentAfter = writer.WrittenSpan.ToArray();
        Assert.Equal(contentBefore, contentAfter);

        // we should be able to fill in at least 100-(10-2) bytes,
        // because we requested 100 span on 8 bytes free buffer:
        for (var i = 0; i < 92; i++)
        {
            span[i] = 42;
        }
    }
}

