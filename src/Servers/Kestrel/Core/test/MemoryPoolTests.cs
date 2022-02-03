// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using Xunit;

namespace Microsoft.Extensions.Internal.Test;

public abstract class MemoryPoolTests
{
    protected abstract MemoryPool<byte> CreatePool();

    [Fact]
    public void CanDisposeAfterCreation()
    {
        var memoryPool = CreatePool();
        memoryPool.Dispose();
    }

    [Fact]
    public void CanDisposeAfterReturningBlock()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        block.Dispose();
        memoryPool.Dispose();
    }

    [Fact]
    public void CanDisposeAfterPinUnpinBlock()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        block.Memory.Pin().Dispose();
        block.Dispose();
        memoryPool.Dispose();
    }

    [Fact]
    public void LeasingFromDisposedPoolThrows()
    {
        var memoryPool = CreatePool();
        memoryPool.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() => memoryPool.Rent());
        Assert.Equal($"Cannot access a disposed object.{Environment.NewLine}Object name: 'MemoryPool'.", exception.Message);
    }
}
