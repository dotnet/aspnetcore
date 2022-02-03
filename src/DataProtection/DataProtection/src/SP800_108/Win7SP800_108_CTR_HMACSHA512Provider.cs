// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.DataProtection.SP800_108;

internal sealed unsafe class Win7SP800_108_CTR_HMACSHA512Provider : ISP800_108_CTR_HMACSHA512Provider
{
    private readonly BCryptHashHandle _hashHandle;

    public Win7SP800_108_CTR_HMACSHA512Provider(byte* pbKdk, uint cbKdk)
    {
        _hashHandle = CachedAlgorithmHandles.HMAC_SHA512.CreateHmac(pbKdk, cbKdk);
    }

    public void DeriveKey(byte* pbLabel, uint cbLabel, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey)
    {
        const uint SHA512_DIGEST_SIZE_IN_BYTES = 512 / 8;
        byte* pbHashDigest = stackalloc byte[(int)SHA512_DIGEST_SIZE_IN_BYTES];

        // NOTE: pbDerivedKey and cbDerivedKey are modified as data is copied to the output buffer.

        // this will be zero-inited
        var tempInputBuffer = new byte[checked(
            sizeof(int) /* [i] */
            + cbLabel /* Label */
            + 1 /* 0x00 */
            + cbContext /* Context */
            + sizeof(int) /* [L] */)];

        fixed (byte* pbTempInputBuffer = tempInputBuffer)
        {
            // Step 1: Calculate all necessary offsets into the temp input & output buffer.
            byte* pbTempInputCounter = pbTempInputBuffer;
            byte* pbTempInputLabel = &pbTempInputCounter[sizeof(int)];
            byte* pbTempInputContext = &pbTempInputLabel[cbLabel + 1 /* 0x00 */];
            byte* pbTempInputBitlengthIndicator = &pbTempInputContext[cbContext];

            // Step 2: Copy Label and Context into the temp input buffer.
            UnsafeBufferUtil.BlockCopy(from: pbLabel, to: pbTempInputLabel, byteCount: cbLabel);
            UnsafeBufferUtil.BlockCopy(from: pbContext, to: pbTempInputContext, byteCount: cbContext);

            // Step 3: copy [L] into last part of data to be hashed, big-endian
            BitHelpers.WriteTo(pbTempInputBitlengthIndicator, checked(cbDerivedKey * 8));

            // Step 4: iterate until all desired bytes have been generated
            for (uint i = 1; cbDerivedKey > 0; i++)
            {
                // Step 4a: Copy [i] into the first part of data to be hashed, big-endian
                BitHelpers.WriteTo(pbTempInputCounter, i);

                // Step 4b: Hash. Win7 doesn't allow reusing hash algorithm objects after the final hash
                // has been computed, so we'll just keep calling DuplicateHash on the original
                // hash handle. This offers a slight performance increase over allocating a new hash
                // handle for each iteration. We don't need to mess with any of this on Win8 since on
                // that platform we use BCryptKeyDerivation directly, which offers superior performance.
                using (var hashHandle = _hashHandle.DuplicateHash())
                {
                    hashHandle.HashData(pbTempInputBuffer, (uint)tempInputBuffer.Length, pbHashDigest, SHA512_DIGEST_SIZE_IN_BYTES);
                }

                // Step 4c: Copy bytes from the temporary buffer to the output buffer.
                uint numBytesToCopy = Math.Min(cbDerivedKey, SHA512_DIGEST_SIZE_IN_BYTES);
                UnsafeBufferUtil.BlockCopy(from: pbHashDigest, to: pbDerivedKey, byteCount: numBytesToCopy);
                pbDerivedKey += numBytesToCopy;
                cbDerivedKey -= numBytesToCopy;
            }
        }
    }

    public void Dispose()
    {
        _hashHandle.Dispose();
    }
}
