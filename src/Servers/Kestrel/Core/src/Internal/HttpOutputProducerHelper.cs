// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    internal class HttpOutputProducerHelper
    {
        public static IMemoryOwner<byte> ReserveFakeMemory(MemoryPool<byte> memoryPool, int sizeHint)
        {
            // Requesting a bigger buffer could throw.
            if (sizeHint <= memoryPool.MaxBufferSize)
            {
                // Use the specified pool as it fits.
                return memoryPool.Rent(sizeHint);
            }
            else
            {
                // Use the array pool. Its MaxBufferSize is int.MaxValue.
                return MemoryPool<byte>.Shared.Rent(sizeHint);
            }
        }
    }
}
