// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace System.Buffers
{
    internal static class SlabMemoryPoolFactory
    {
        private static MemoryPool<byte> s_memoryPool;

        public static MemoryPool<byte> Singleton()
        {
            return s_memoryPool ?? CreateSingleton();
        }

        private static MemoryPool<byte> CreateSingleton()
        {
#if DEBUG
            var memoryPool = new DiagnosticMemoryPool(CreateSlabMemoryPool());
#else
            var memoryPool = CreateSlabMemoryPool();
#endif

            if (Interlocked.CompareExchange(ref s_memoryPool, memoryPool, null) == null)
            {
                return memoryPool;
            };

            return s_memoryPool;
        }

        public static MemoryPool<byte> Create()
        {
#if DEBUG
            return new DiagnosticMemoryPool(CreateSlabMemoryPool());
#else
            return CreateSlabMemoryPool();
#endif
        }

        public static MemoryPool<byte> CreateSlabMemoryPool()
        {
            return new SlabMemoryPool();
        }
    }
}
