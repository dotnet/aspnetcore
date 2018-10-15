// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.SP800_108
{
    internal unsafe static class SP800_108_CTR_HMACSHA512Extensions
    {
        public static void DeriveKeyWithContextHeader(this ISP800_108_CTR_HMACSHA512Provider provider, byte* pbLabel, uint cbLabel, byte[] contextHeader, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey)
        {
            var cbCombinedContext = checked((uint)contextHeader.Length + cbContext);

            // Try allocating the combined context on the stack to avoid temporary managed objects; only fall back to heap if buffers are too large.
            byte[] heapAllocatedCombinedContext = (cbCombinedContext > Constants.MAX_STACKALLOC_BYTES) ? new byte[cbCombinedContext] : null;
            fixed (byte* pbHeapAllocatedCombinedContext = heapAllocatedCombinedContext)
            {
                byte* pbCombinedContext = pbHeapAllocatedCombinedContext;
                if (pbCombinedContext == null)
                {
                    byte* pbStackAllocatedCombinedContext = stackalloc byte[(int)cbCombinedContext]; // will be released when frame pops
                    pbCombinedContext = pbStackAllocatedCombinedContext;
                }

                fixed (byte* pbContextHeader = contextHeader)
                {
                    UnsafeBufferUtil.BlockCopy(from: pbContextHeader, to: pbCombinedContext, byteCount: contextHeader.Length);
                }
                UnsafeBufferUtil.BlockCopy(from: pbContext, to: &pbCombinedContext[contextHeader.Length], byteCount: cbContext);

                // At this point, combinedContext := { contextHeader || context }
                provider.DeriveKey(pbLabel, cbLabel, pbCombinedContext, cbCombinedContext, pbDerivedKey, cbDerivedKey);
            }
        }
    }
}
