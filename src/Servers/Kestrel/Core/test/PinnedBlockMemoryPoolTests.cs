// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace Microsoft.Extensions.Internal.Test;

public class PinnedBlockMemoryPoolTests : MemoryPoolTests
{
    protected override MemoryPool<byte> CreatePool() => new PinnedBlockMemoryPool();

    [Fact]
    public void DoubleDisposeWorks()
    {
        var memoryPool = CreatePool();
        memoryPool.Dispose();
        memoryPool.Dispose();
    }

    [Fact]
    public void DisposeWithActiveBlocksWorks()
    {
        var memoryPool = CreatePool();
        var block = memoryPool.Rent();
        memoryPool.Dispose();
    }
}
