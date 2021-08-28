// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    internal class HttpOutputProducerHelper
    {
        public static IMemoryOwner<byte> ReserveFakeMemory(MemoryPool<byte> memoryPool, int minSize)
        {
            // Requesting a bigger buffer could throw.
            if (minSize <= memoryPool.MaxBufferSize)
            {
                // Use the specified pool as it fits.
                return memoryPool.Rent(minSize);
            }
            else
            {
                // Use the array pool. Its MaxBufferSize is int.MaxValue.
                return MemoryPool<byte>.Shared.Rent(minSize);
            }
        }
    }
}
