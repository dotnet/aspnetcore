// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Shared.Tests.Buffers;

public class RefPooledArrayBufferWriterTests
{
    [Theory]
    [InlineData(16)]
    [InlineData(256)]
    [InlineData(1024)]
    public void Constructor_WithInitialBuffer_InitializesCorrectly(int initialCapacity)
    {
        Span<byte> initialBuffer = stackalloc byte[initialCapacity];
        var writer = new RefPooledArrayBufferWriter<byte>(initialBuffer);

        var span = writer.GetSpan();
        Assert.True(span.Length >= initialCapacity);

        writer.Dispose();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Advance_WithNegativeCount_Throws(int negativeCount)
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        // ref struct cannot be captured in lambda, so using try-catch
        try
        {
            writer.Advance(negativeCount);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void Advance_BeyondCapacity_Throws()
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        // ref struct cannot be captured in lambda, use try-catch
        try
        {
            writer.Advance(20);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void GetSpan_WithInvalidSizeHint_Throws()
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        // ref struct cannot be captured in lambda, use try-catch
        try
        {
            writer.GetSpan(-1);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
        finally
        {
            writer.Dispose();
        }
    }

    [Fact]
    public void GetMemory_ThrowsNotSupported()
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        // ref struct cannot be captured in lambda, use try-catch
        try
        {
            writer.GetMemory();
            Assert.Fail("Expected NotSupportedException or UnreachableException");
        }
#if NET
        catch (UnreachableException)
        {
            // Expected (for .NET Core)
        }
#else
        catch (NotSupportedException)
        {
            // Expected (for .NET Framework and older versions)
        }
#endif
        finally
        {
            writer.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void GetSpan_WithSizeHint_ResizesBufferIfNeeded(int sizeHint)
    {
        Span<byte> buffer = stackalloc byte[10];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        var span = writer.GetSpan(sizeHint);
        Assert.True(span.Length >= sizeHint && span.Length > 0);

        writer.Dispose();
    }

    [Fact]
    public void WrittenSpan_ReturnsSpanOfWrittenData()
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        var writtenSpan = writer.WrittenSpan;
        Assert.Equal(2, writtenSpan.Length);
        Assert.Equal(42, writtenSpan[0]);
        Assert.Equal(99, writtenSpan[1]);

        writer.Dispose();
    }

    [Fact]
    public void BufferPreservesContentOnGrowth_FromStackToPooledBuffer()
    {
        var initialCapacity = 10;
        Span<byte> initialBuffer = stackalloc byte[initialCapacity];
        var writer = new RefPooledArrayBufferWriter<byte>(initialBuffer);

        var span = writer.GetSpan();
        span[0] = 42;
        span[1] = 99;
        writer.Advance(2);

        var contentBefore = writer.WrittenSpan.ToArray();

        // Force buffer growth from stack to pooled array
        span = writer.GetSpan(100);

        var contentAfter = writer.WrittenSpan.ToArray();
        Assert.Equal(contentBefore, contentAfter);

        // Verify we can write at least 92 more bytes (100 - 8 bytes remaining)
        for (var i = 0; i < 92; i++)
        {
            span[i] = (byte)(i % 256);
        }
        writer.Advance(92);

        Assert.Equal(94, writer.WrittenSpan.Length);

        writer.Dispose();
    }

    [Fact]
    public void Buffer_AdvancesOnBoundaryLimits()
    {
        Span<byte> initialBuffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(initialBuffer);

        _ = writer.GetSpan();
        writer.Advance(256); // should not throw
        Assert.Equal(256, writer.WrittenSpan.Length);
    }

    [Fact]
    public void BufferPreservesContentOnGrowth_PooledToLargerPooledBuffer()
    {
        Span<byte> initialBuffer = stackalloc byte[16];
        var writer = new RefPooledArrayBufferWriter<byte>(initialBuffer);

        // Force initial growth to pooled buffer
        var span = writer.GetSpan(100);
        for (var i = 0; i < 50; i++)
        {
            span[i] = (byte)(i % 256);
        }
        writer.Advance(50);

        var contentBefore = writer.WrittenSpan.ToArray();

        // Force second growth from pooled to larger pooled
        span = writer.GetSpan(500);

        var contentAfter = writer.WrittenSpan.ToArray();
        Assert.Equal(contentBefore, contentAfter);

        // Verify content integrity
        for (var i = 0; i < 50; i++)
        {
            Assert.Equal((byte)(i % 256), contentAfter[i]);
        }

        writer.Dispose();
    }

    [Fact]
    public void MultipleWrites_WorkCorrectly()
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        // First write
        var span = writer.GetSpan();
        span[0] = 1;
        span[1] = 2;
        writer.Advance(2);

        // Second write
        span = writer.GetSpan();
        span[0] = 3;
        span[1] = 4;
        writer.Advance(2);

        // Third write
        span = writer.GetSpan();
        span[0] = 5;
        writer.Advance(1);

        var written = writer.WrittenSpan;
        Assert.Equal(5, written.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, written.ToArray());

        writer.Dispose();
    }

    [Fact]
    public void GetSpan_ReturnsConsistentReference_BeforeAdvance()
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        var span1 = writer.GetSpan();
        span1[0] = 42;

        var span2 = writer.GetSpan();
        Assert.Equal(42, span2[0]); // Should see the same buffer

        writer.Advance(1);
        Assert.Equal(42, writer.WrittenSpan[0]);

        writer.Dispose();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void LargeWrite_AcrossGrowthBoundary_PreservesData(int iterations)
    {
        Span<byte> buffer = stackalloc byte[8];
        var writer = new RefPooledArrayBufferWriter<byte>(buffer);

        for (int i = 0; i < iterations; i++)
        {
            var span = writer.GetSpan(10);
            for (int j = 0; j < 10; j++)
            {
                span[j] = (byte)((i * 10 + j) % 256);
            }
            writer.Advance(10);
        }

        var written = writer.WrittenSpan;
        Assert.Equal(iterations * 10, written.Length);

        // Verify all data
        for (int i = 0; i < iterations * 10; i++)
        {
            Assert.Equal((byte)(i % 256), written[i]);
        }

        writer.Dispose();
    }
}
