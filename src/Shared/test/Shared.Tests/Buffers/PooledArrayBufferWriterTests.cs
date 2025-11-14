// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Xunit;

namespace Microsoft.AspNetCore.Shared.Tests.Buffers;

public class PooledArrayBufferWriterTests
{
    [Fact]
    public void Constructor_WithoutInitialCapacity_InitializesWithMinimumBufferSize()
    {
        using var writer = new PooledArrayBufferWriter<byte>();

        Assert.Equal(0, writer.WrittenCount);
        Assert.True(writer.Capacity >= 256);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(1024)]
    public void Constructor_WithInitialCapacity_InitializesWithRequestedCapacity(int initialCapacity)
    {
        using var writer = new PooledArrayBufferWriter<byte>(initialCapacity);

        Assert.Equal(0, writer.WrittenCount);
        Assert.True(writer.Capacity >= initialCapacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidInitialCapacity_Throws(int invalidCapacity)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PooledArrayBufferWriter<byte>(invalidCapacity));
        Assert.Equal(nameof(invalidCapacity), ex.ParamName);
    }

    [Fact]
    public void WrittenCount_ReturnsNumberOfBytesWritten()
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        var span = writer.GetSpan(10);
        span[0] = 1;
        span[1] = 2;
        writer.Advance(2);

        Assert.Equal(2, writer.WrittenCount);
    }

    [Fact]
    public void WrittenCount_AfterClear_ReturnsZero()
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        writer.GetSpan(10)[0] = 1;
        writer.Advance(1);
        writer.Clear();

        Assert.Equal(0, writer.WrittenCount);
    }

    [Fact]
    public void Capacity_ReturnsBufferCapacity()
    {
        using var writer = new PooledArrayBufferWriter<byte>(512);

        Assert.True(writer.Capacity >= 512);
    }

    [Fact]
    public void FreeCapacity_ReturnsRemainingSpace()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        writer.Advance(100);

        Assert.Equal(writer.Capacity - 100, writer.FreeCapacity);
    }

    [Fact]
    public void GetSpan_WithoutSizeHint_ReturnsSpan()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        var span = writer.GetSpan();

        Assert.True(span.Length > 0);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetSpan_WithSizeHint_ResizesBufferIfNeeded(int sizeHint)
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        var span = writer.GetSpan(sizeHint);

        Assert.True(span.Length >= sizeHint);
    }

    [Fact]
    public void GetSpan_MultipleCallsWithoutAdvance_ReturnsSameSpan()
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        var span1 = writer.GetSpan();
        var span2 = writer.GetSpan();

        Assert.Equal(span1.Length, span2.Length);
    }

    [Fact]
    public void GetMemory_WithoutSizeHint_ReturnsMemory()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        var memory = writer.GetMemory();

        Assert.True(memory.Length > 0);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetMemory_WithSizeHint_ResizesBufferIfNeeded(int sizeHint)
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        var memory = writer.GetMemory(sizeHint);

        Assert.True(memory.Length >= sizeHint);
    }

    [Fact]
    public void Advance_WithValidCount_IncrementsWrittenCount()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        writer.Advance(10);

        Assert.Equal(10, writer.WrittenCount);
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

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Advance_WithNegativeCount_Throws(int negativeCount)
    {
        using var writer = new PooledArrayBufferWriter<byte>();
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(negativeCount));
        Assert.Equal(nameof(negativeCount), ex.ParamName);
    }

    [Fact]
    public void Advance_BeyondCapacity_Throws()
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        Assert.Throws<InvalidOperationException>(() => writer.Advance(20));
    }

    [Fact]
    public void Advance_WithUint_ConvertsAndAdvances()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);
        writer.Advance((uint)5);

        Assert.Equal(5, writer.WrittenCount);
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
    }

    [Fact]
    public void GetSpan_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan());
    }

    [Fact]
    public void GetMemory_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.GetMemory());
    }

    [Fact]
    public void WrittenSpan_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenSpan);
    }

    [Fact]
    public void WrittenMemory_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenMemory);
    }

    [Fact]
    public void WrittenCount_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = writer.WrittenCount);
    }

    [Fact]
    public void Capacity_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = writer.Capacity);
    }

    [Fact]
    public void FreeCapacity_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = writer.FreeCapacity);
    }

    [Fact]
    public void Advance_AfterDispose_Throws()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.Advance(1));
    }

    [Fact]
    public void MultipleWrites_WorkCorrectly()
    {
        using var writer = new PooledArrayBufferWriter<byte>(256);

        var span1 = writer.GetSpan();
        span1[0] = 1;
        writer.Advance(1);

        var span2 = writer.GetSpan();
        span2[0] = 2;
        writer.Advance(1);

        var writtenSpan = writer.WrittenSpan;
        Assert.Equal(2, writtenSpan.Length);
        Assert.Equal(1, writtenSpan[0]);
        Assert.Equal(2, writtenSpan[1]);
    }

    [Fact]
    public void BufferGrowth_WorksCorrectly()
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);
        var initialCapacity = writer.Capacity;

        writer.GetSpan(100);

        Assert.True(writer.Capacity > initialCapacity);
    }

    [Fact]
    public void BufferPreservesContentOnGrowth()
    {
        using var writer = new PooledArrayBufferWriter<byte>(10);

        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        var contentBefore = writer.WrittenSpan.ToArray();

        // Force buffer growth
        writer.GetSpan(100);

        var contentAfter = writer.WrittenSpan.ToArray();
        Assert.Equal(contentBefore, contentAfter);
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
    public void WriterWithDifferentTypes_Works()
    {
        using var writer = new PooledArrayBufferWriter<int>();
        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        Assert.Equal(2, writer.WrittenCount);
        Assert.Equal(42, writer.WrittenSpan[0]);
        Assert.Equal(99, writer.WrittenSpan[1]);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var writer = new PooledArrayBufferWriter<byte>();
        writer.Dispose();
        writer.Dispose(); // Should not throw
    }
}

