// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;

/// <summary>
/// A PBKDF2 provider which utilizes the Win8 API BCryptKeyDerivation.
/// </summary>
internal sealed unsafe class Win8Pbkdf2Provider : IPbkdf2Provider
{
    public byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
    {
        Debug.Assert(password != null);
        Debug.Assert(salt != null);
        Debug.Assert(iterationCount > 0);
        Debug.Assert(numBytesRequested > 0);

        string algorithmName = PrfToCngAlgorithmId(prf);
        fixed (byte* pbHeapAllocatedSalt = salt)
        {
            byte dummy; // CLR doesn't like pinning zero-length buffers, so this provides a valid memory address when working with zero-length buffers
            byte* pbSalt = (pbHeapAllocatedSalt != null) ? pbHeapAllocatedSalt : &dummy;

            byte[] retVal = new byte[numBytesRequested];
            using (BCryptKeyHandle keyHandle = PasswordToPbkdfKeyHandle(password, CachedAlgorithmHandles.PBKDF2, prf))
            {
                fixed (byte* pbRetVal = retVal)
                {
                    DeriveKeyCore(keyHandle, algorithmName, pbSalt, (uint)salt.Length, (ulong)iterationCount, pbRetVal, (uint)retVal.Length);
                }
                return retVal;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetTotalByteLengthIncludingNullTerminator(string input)
    {
        if (input == null)
        {
            // degenerate case
            return 0;
        }
        else
        {
            uint numChars = (uint)input.Length + 1U; // no overflow check necessary since Length is signed
            return checked(numChars * sizeof(char));
        }
    }

    private static BCryptKeyHandle PasswordToPbkdfKeyHandle(string password, BCryptAlgorithmHandle pbkdf2AlgHandle, KeyDerivationPrf prf)
    {
        byte dummy; // CLR doesn't like pinning zero-length buffers, so this provides a valid memory address when working with zero-length buffers

        // Convert password string to bytes.
        // Allocate on the stack whenever we can to save allocations.
        int cbPasswordBuffer = Encoding.UTF8.GetMaxByteCount(password.Length);
        fixed (byte* pbHeapAllocatedPasswordBuffer = (cbPasswordBuffer > Constants.MAX_STACKALLOC_BYTES) ? new byte[cbPasswordBuffer] : null)
        {
            byte* pbPasswordBuffer = pbHeapAllocatedPasswordBuffer;
            if (pbPasswordBuffer == null)
            {
                if (cbPasswordBuffer == 0)
                {
                    pbPasswordBuffer = &dummy;
                }
                else
                {
                    byte* pbStackAllocPasswordBuffer = stackalloc byte[cbPasswordBuffer]; // will be released when the frame unwinds
                    pbPasswordBuffer = pbStackAllocPasswordBuffer;
                }
            }

            try
            {
                int cbPasswordBufferUsed; // we're not filling the entire buffer, just a partial buffer
                fixed (char* pszPassword = password)
                {
                    cbPasswordBufferUsed = Encoding.UTF8.GetBytes(pszPassword, password.Length, pbPasswordBuffer, cbPasswordBuffer);
                }

                return PasswordToPbkdfKeyHandleStep2(pbkdf2AlgHandle, pbPasswordBuffer, (uint)cbPasswordBufferUsed, prf);
            }
            finally
            {
                UnsafeBufferUtil.SecureZeroMemory(pbPasswordBuffer, cbPasswordBuffer);
            }
        }
    }

    private static BCryptKeyHandle PasswordToPbkdfKeyHandleStep2(BCryptAlgorithmHandle pbkdf2AlgHandle, byte* pbPassword, uint cbPassword, KeyDerivationPrf prf)
    {
        const uint PBKDF2_MAX_KEYLENGTH_IN_BYTES = 2048; // GetSupportedKeyLengths() on a Win8 box; value should never be lowered in any future version of Windows
        if (cbPassword <= PBKDF2_MAX_KEYLENGTH_IN_BYTES)
        {
            // Common case: the password is small enough to be consumed directly by the PBKDF2 algorithm.
            return pbkdf2AlgHandle.GenerateSymmetricKey(pbPassword, cbPassword);
        }
        else
        {
            // Rare case: password is very long; we must hash manually.
            // PBKDF2 uses the PRFs in HMAC mode, and when the HMAC input key exceeds the hash function's
            // block length the key is hashed and run back through the key initialization function.

            BCryptAlgorithmHandle prfAlgorithmHandle; // cached; don't dispose
            switch (prf)
            {
                case KeyDerivationPrf.HMACSHA1:
                    prfAlgorithmHandle = CachedAlgorithmHandles.SHA1;
                    break;
                case KeyDerivationPrf.HMACSHA256:
                    prfAlgorithmHandle = CachedAlgorithmHandles.SHA256;
                    break;
                case KeyDerivationPrf.HMACSHA512:
                    prfAlgorithmHandle = CachedAlgorithmHandles.SHA512;
                    break;
                default:
                    throw CryptoUtil.Fail("Unrecognized PRF.");
            }

            // Final sanity check: don't hash the password if the HMAC key initialization function wouldn't have done it for us.
            if (cbPassword <= prfAlgorithmHandle.GetHashBlockLength() /* in bytes */)
            {
                return pbkdf2AlgHandle.GenerateSymmetricKey(pbPassword, cbPassword);
            }

            // Hash the password and use the hash as input to PBKDF2.
            uint cbPasswordDigest = prfAlgorithmHandle.GetHashDigestLength();
            CryptoUtil.Assert(cbPasswordDigest > 0, "cbPasswordDigest > 0");
            fixed (byte* pbPasswordDigest = new byte[cbPasswordDigest])
            {
                try
                {
                    using (var hashHandle = prfAlgorithmHandle.CreateHash())
                    {
                        hashHandle.HashData(pbPassword, cbPassword, pbPasswordDigest, cbPasswordDigest);
                    }
                    return pbkdf2AlgHandle.GenerateSymmetricKey(pbPasswordDigest, cbPasswordDigest);
                }
                finally
                {
                    UnsafeBufferUtil.SecureZeroMemory(pbPasswordDigest, cbPasswordDigest);
                }
            }
        }
    }

    private static void DeriveKeyCore(BCryptKeyHandle pbkdf2KeyHandle, string hashAlgorithm, byte* pbSalt, uint cbSalt, ulong iterCount, byte* pbDerivedBytes, uint cbDerivedBytes)
    {
        // First, build the buffers necessary to pass (hash alg, salt, iter count) into the KDF
        BCryptBuffer* pBuffers = stackalloc BCryptBuffer[3];

        pBuffers[0].BufferType = BCryptKeyDerivationBufferType.KDF_ITERATION_COUNT;
        pBuffers[0].pvBuffer = (IntPtr)(&iterCount);
        pBuffers[0].cbBuffer = sizeof(ulong);

        pBuffers[1].BufferType = BCryptKeyDerivationBufferType.KDF_SALT;
        pBuffers[1].pvBuffer = (IntPtr)pbSalt;
        pBuffers[1].cbBuffer = cbSalt;

        fixed (char* pszHashAlgorithm = hashAlgorithm)
        {
            pBuffers[2].BufferType = BCryptKeyDerivationBufferType.KDF_HASH_ALGORITHM;
            pBuffers[2].pvBuffer = (IntPtr)pszHashAlgorithm;
            pBuffers[2].cbBuffer = GetTotalByteLengthIncludingNullTerminator(hashAlgorithm);

            // Add the header which points to the buffers
            BCryptBufferDesc bufferDesc = default(BCryptBufferDesc);
            BCryptBufferDesc.Initialize(ref bufferDesc);
            bufferDesc.cBuffers = 3;
            bufferDesc.pBuffers = pBuffers;

            // Finally, import the KDK into the KDF algorithm, then invoke the KDF
            uint numBytesDerived;
            int ntstatus = UnsafeNativeMethods.BCryptKeyDerivation(
                    hKey: pbkdf2KeyHandle,
                    pParameterList: &bufferDesc,
                    pbDerivedKey: pbDerivedBytes,
                    cbDerivedKey: cbDerivedBytes,
                    pcbResult: out numBytesDerived,
                    dwFlags: 0);
            UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);

            // Final sanity checks before returning control to caller.
            CryptoUtil.Assert(numBytesDerived == cbDerivedBytes, "numBytesDerived == cbDerivedBytes");
        }
    }

    private static string PrfToCngAlgorithmId(KeyDerivationPrf prf)
    {
        switch (prf)
        {
            case KeyDerivationPrf.HMACSHA1:
                return Constants.BCRYPT_SHA1_ALGORITHM;
            case KeyDerivationPrf.HMACSHA256:
                return Constants.BCRYPT_SHA256_ALGORITHM;
            case KeyDerivationPrf.HMACSHA512:
                return Constants.BCRYPT_SHA512_ALGORITHM;
            default:
                throw CryptoUtil.Fail("Unrecognized PRF.");
        }
    }
}
