// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Xunit;

namespace Microsoft.Extensions.Internal.Test
{
    public  class SlabMemoryPoolTests: MemoryPoolTests
    {
        protected override MemoryPool<byte> CreatePool() => new SlabMemoryPool();

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
}