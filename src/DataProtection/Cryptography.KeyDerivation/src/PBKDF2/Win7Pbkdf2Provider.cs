// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;

/// <summary>
/// A PBKDF2 provider which utilizes the Win7 API BCryptDeriveKeyPBKDF2.
/// </summary>
internal sealed unsafe class Win7Pbkdf2Provider : IPbkdf2Provider
{
    public byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
    {
        Debug.Assert(password != null);
        Debug.Assert(salt != null);
        Debug.Assert(iterationCount > 0);
        Debug.Assert(numBytesRequested > 0);

        byte dummy; // CLR doesn't like pinning zero-length buffers, so this provides a valid memory address when working with zero-length buffers

        // Don't dispose of this algorithm instance; it is cached and reused!
        var algHandle = PrfToCachedCngAlgorithmInstance(prf);

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

                fixed (byte* pbHeapAllocatedSalt = salt)
                {
                    byte* pbSalt = (pbHeapAllocatedSalt != null) ? pbHeapAllocatedSalt : &dummy;

                    byte[] retVal = new byte[numBytesRequested];
                    fixed (byte* pbRetVal = retVal)
                    {
                        int ntstatus = UnsafeNativeMethods.BCryptDeriveKeyPBKDF2(
                            hPrf: algHandle,
                            pbPassword: pbPasswordBuffer,
                            cbPassword: (uint)cbPasswordBufferUsed,
                            pbSalt: pbSalt,
                            cbSalt: (uint)salt.Length,
                            cIterations: (ulong)iterationCount,
                            pbDerivedKey: pbRetVal,
                            cbDerivedKey: (uint)retVal.Length,
                            dwFlags: 0);
                        UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
                    }
                    return retVal;
                }
            }
            finally
            {
                UnsafeBufferUtil.SecureZeroMemory(pbPasswordBuffer, cbPasswordBuffer);
            }
        }
    }

    private static BCryptAlgorithmHandle PrfToCachedCngAlgorithmInstance(KeyDerivationPrf prf)
    {
        switch (prf)
        {
            case KeyDerivationPrf.HMACSHA1:
                return CachedAlgorithmHandles.HMAC_SHA1;
            case KeyDerivationPrf.HMACSHA256:
                return CachedAlgorithmHandles.HMAC_SHA256;
            case KeyDerivationPrf.HMACSHA512:
                return CachedAlgorithmHandles.HMAC_SHA512;
            default:
                throw CryptoUtil.Fail("Unrecognized PRF.");
        }
    }
}
