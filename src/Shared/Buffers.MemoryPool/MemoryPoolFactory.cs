// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Buffers
{
    internal static class PinnedBlockMemoryPoolFactory
    {
        public static MemoryPool<byte> Create()
        {
#if DEBUG
            return new DiagnosticMemoryPool(CreatePinnedBlockMemoryPool());
#else
            return CreatePinnedBlockMemoryPool();
#endif
        }

        public static MemoryPool<byte> CreatePinnedBlockMemoryPool()
        {
            return new PinnedBlockMemoryPool();
        }
    }
}
