// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.DataProtection.SP800_108;

internal sealed unsafe class Win8SP800_108_CTR_HMACSHA512Provider : ISP800_108_CTR_HMACSHA512Provider
{
    private readonly BCryptKeyHandle _keyHandle;

    public Win8SP800_108_CTR_HMACSHA512Provider(byte* pbKdk, uint cbKdk)
    {
        _keyHandle = ImportKey(pbKdk, cbKdk);
    }

    public void DeriveKey(byte* pbLabel, uint cbLabel, byte* pbContext, uint cbContext, byte* pbDerivedKey, uint cbDerivedKey)
    {
        const int SHA512_ALG_CHAR_COUNT = 7;
        char* pszHashAlgorithm = stackalloc char[SHA512_ALG_CHAR_COUNT /* includes terminating null */];
        pszHashAlgorithm[0] = 'S';
        pszHashAlgorithm[1] = 'H';
        pszHashAlgorithm[2] = 'A';
        pszHashAlgorithm[3] = '5';
        pszHashAlgorithm[4] = '1';
        pszHashAlgorithm[5] = '2';
        pszHashAlgorithm[6] = (char)0;

        // First, build the buffers necessary to pass (label, context, PRF algorithm) into the KDF
        BCryptBuffer* pBuffers = stackalloc BCryptBuffer[3];

        pBuffers[0].BufferType = BCryptKeyDerivationBufferType.KDF_LABEL;
        pBuffers[0].pvBuffer = (IntPtr)pbLabel;
        pBuffers[0].cbBuffer = cbLabel;

        pBuffers[1].BufferType = BCryptKeyDerivationBufferType.KDF_CONTEXT;
        pBuffers[1].pvBuffer = (IntPtr)pbContext;
        pBuffers[1].cbBuffer = cbContext;

        pBuffers[2].BufferType = BCryptKeyDerivationBufferType.KDF_HASH_ALGORITHM;
        pBuffers[2].pvBuffer = (IntPtr)pszHashAlgorithm;
        pBuffers[2].cbBuffer = checked(SHA512_ALG_CHAR_COUNT * sizeof(char));

        // Add the header which points to the buffers
        var bufferDesc = default(BCryptBufferDesc);
        BCryptBufferDesc.Initialize(ref bufferDesc);
        bufferDesc.cBuffers = 3;
        bufferDesc.pBuffers = pBuffers;

        // Finally, invoke the KDF
        uint numBytesDerived;
        var ntstatus = UnsafeNativeMethods.BCryptKeyDerivation(
            hKey: _keyHandle,
            pParameterList: &bufferDesc,
            pbDerivedKey: pbDerivedKey,
            cbDerivedKey: cbDerivedKey,
            pcbResult: out numBytesDerived,
            dwFlags: 0);
        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

        // Final sanity checks before returning control to caller.
        CryptoUtil.Assert(numBytesDerived == cbDerivedKey, "numBytesDerived == cbDerivedKey");
    }

    public void Dispose()
    {
        _keyHandle.Dispose();
    }

    private static BCryptKeyHandle ImportKey(byte* pbKdk, uint cbKdk)
    {
        // The MS implementation of SP800_108_CTR_HMAC has a limit on the size of the key it can accept.
        // If the incoming key is too long, we'll hash it using SHA512 to bring it back to a manageable
        // length. This transform is appropriate since SP800_108_CTR_HMAC is just a glorified HMAC under
        // the covers, and the HMAC algorithm allows hashing the key using the underlying PRF if the key
        // is greater than the PRF's block length.

        const uint SHA512_BLOCK_SIZE_IN_BYTES = 1024 / 8;
        const uint SHA512_DIGEST_SIZE_IN_BYTES = 512 / 8;

        if (cbKdk > SHA512_BLOCK_SIZE_IN_BYTES)
        {
            // Hash key.
            byte* pbHashedKey = stackalloc byte[(int)SHA512_DIGEST_SIZE_IN_BYTES];
            try
            {
                using (var hashHandle = CachedAlgorithmHandles.SHA512.CreateHash())
                {
                    hashHandle.HashData(pbKdk, cbKdk, pbHashedKey, SHA512_DIGEST_SIZE_IN_BYTES);
                }
                return CachedAlgorithmHandles.SP800_108_CTR_HMAC.GenerateSymmetricKey(pbHashedKey, SHA512_DIGEST_SIZE_IN_BYTES);
            }
            finally
            {
                UnsafeBufferUtil.SecureZeroMemory(pbHashedKey, SHA512_DIGEST_SIZE_IN_BYTES);
            }
        }
        else
        {
            // Use key directly.
            return CachedAlgorithmHandles.SP800_108_CTR_HMAC.GenerateSymmetricKey(pbKdk, cbKdk);
        }
    }
}
