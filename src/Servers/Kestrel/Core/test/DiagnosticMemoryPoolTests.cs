// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Internal.Test;

public class DiagnosticMemoryPoolTests : MemoryPoolTests
{
    protected override MemoryPool<byte> CreatePool() => new DiagnosticMemoryPool(new PinnedBlockMemoryPool());

    [Fact]
    public void DoubleDisposeThrows()
    {
        var memoryPool = CreatePool();
        memoryPool.Dispose();
        var exception = Assert.Throws<InvalidOperationException>(() => memoryPool.Dispose());
        Assert.Equal("Object is being disposed twice", exception.Message);
    }

    [Fact]
    public void DisposeWithActiveBlocksThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        ExpectDisposeException(memoryPool);

        var exception = Assert.Throws<InvalidOperationException>(() => block.Dispose());
        Assert.Equal("Block is being returned to disposed pool", exception.Message);
    }

    [Fact]
    public void DoubleBlockDisposeThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        block.Dispose();
        var exception = Assert.Throws<InvalidOperationException>(() => block.Dispose());
        Assert.Equal("Block is being disposed twice", exception.Message);

        ExpectDisposeAggregateException(memoryPool, exception);
    }

    [Fact]
    public void GetMemoryOfDisposedPoolThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();

        ExpectDisposeException(memoryPool);

        var exception = Assert.Throws<InvalidOperationException>(() => block.Memory);
        Assert.Equal("Block is backed by disposed slab", exception.Message);
    }

    [Fact]
    public void GetMemoryPinOfDisposedPoolThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        var memory = block.Memory;

        ExpectDisposeException(memoryPool);

        var exception = Assert.Throws<InvalidOperationException>(() => memory.Pin());
        Assert.Equal("Block is backed by disposed slab", exception.Message);
    }

    [Fact]
    public void GetMemorySpanOfDisposedPoolThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        var memory = block.Memory;

        ExpectDisposeException(memoryPool);

        var threw = false;
        try
        {
            _ = memory.Span;
        }
        catch (InvalidOperationException ode)
        {
            threw = true;
            Assert.Equal("Block is backed by disposed slab", ode.Message);
        }
        Assert.True(threw);
    }

    [Fact]
    public void GetMemoryTryGetArrayOfDisposedPoolThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        var memory = block.Memory;

        ExpectDisposeException(memoryPool);

        var exception = Assert.Throws<InvalidOperationException>(() => MemoryMarshal.TryGetArray<byte>(memory, out _));
        Assert.Equal("Block is backed by disposed slab", exception.Message);
    }

    [Fact]
    public void GetMemoryOfDisposedThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();

        block.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() => block.Memory);
        Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", exception.Message);

        ExpectDisposeAggregateException(memoryPool, exception);
    }

    [Fact]
    public void GetMemoryPinOfDisposedThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        var memory = block.Memory;

        block.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() => memory.Pin());
        Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", exception.Message);

        ExpectDisposeAggregateException(memoryPool, exception);
    }

    [Fact]
    public void GetMemorySpanOfDisposedThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        var memory = block.Memory;

        block.Dispose();

        Exception exception = null;
        try
        {
            _ = memory.Span;
        }
        catch (ObjectDisposedException ode)
        {
            exception = ode;
            Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", ode.Message);
        }
        Assert.NotNull(exception);

        ExpectDisposeAggregateException(memoryPool, exception);
    }

    [Fact]
    public void GetMemoryTryGetArrayOfDisposedThrows()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        var memory = block.Memory;

        block.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() => MemoryMarshal.TryGetArray<byte>(memory, out _));
        Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPoolBlock'.", exception.Message);

        ExpectDisposeAggregateException(memoryPool, exception);
    }

    [Fact]
    public async Task DoesNotThrowWithLateReturns()
    {
        var memoryPool = new DiagnosticMemoryPool(new PinnedBlockMemoryPool(), allowLateReturn: true);
        var block = memoryPool.Rent();
        memoryPool.Dispose();
        block.Dispose();
        await memoryPool.WhenAllBlocksReturnedAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ThrowsOnAccessToLateBlocks()
    {
        var memoryPool = new DiagnosticMemoryPool(new PinnedBlockMemoryPool(), allowLateReturn: true);
        var block = memoryPool.Rent();
        memoryPool.Dispose();

        var exception = Assert.Throws<InvalidOperationException>(() => block.Memory);
        Assert.Equal("Block is backed by disposed slab", exception.Message);

        block.Dispose();
        var aggregateException = await Assert.ThrowsAsync<AggregateException>(async () => await memoryPool.WhenAllBlocksReturnedAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(new Exception[] { exception }, aggregateException.InnerExceptions);
    }

    [Fact]
    public void ExceptionsContainStackTraceWhenEnabled()
    {
        var memoryPool = new DiagnosticMemoryPool(new PinnedBlockMemoryPool(), rentTracking: true);
        var block = memoryPool.Rent();

        ExpectDisposeException(memoryPool);

        var exception = Assert.Throws<InvalidOperationException>(() => block.Memory);
        Assert.Contains("Block is backed by disposed slab", exception.Message);
        Assert.Contains("ExceptionsContainStackTraceWhenEnabled", exception.Message);
    }

    private static void ExpectDisposeException(MemoryPool<byte> memoryPool)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => memoryPool.Dispose());
        Assert.Contains("Memory pool with active blocks is being disposed, 0 of 1 returned", exception.Message);
    }

    private static void ExpectDisposeAggregateException(MemoryPool<byte> memoryPool, params Exception[] inner)
    {
        var exception = Assert.Throws<AggregateException>(() => memoryPool.Dispose());

        Assert.Equal(inner, exception.InnerExceptions);
    }
}
