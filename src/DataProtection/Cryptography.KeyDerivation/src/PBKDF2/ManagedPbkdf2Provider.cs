// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;

/// <summary>
/// A PBKDF2 provider which utilizes the managed hash algorithm classes as PRFs.
/// This isn't the preferred provider since the implementation is slow, but it is provided as a fallback.
/// </summary>
internal sealed class ManagedPbkdf2Provider : IPbkdf2Provider
{
    public byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
    {
        Debug.Assert(password != null);
        Debug.Assert(salt != null);
        Debug.Assert(iterationCount > 0);
        Debug.Assert(numBytesRequested > 0);

        // PBKDF2 is defined in NIST SP800-132, Sec. 5.3.
        // http://csrc.nist.gov/publications/nistpubs/800-132/nist-sp800-132.pdf

        byte[] retVal = new byte[numBytesRequested];
        int numBytesWritten = 0;
        int numBytesRemaining = numBytesRequested;

        // For each block index, U_0 := Salt || block_index
        byte[] saltWithBlockIndex = new byte[checked(salt.Length + sizeof(uint))];
        Buffer.BlockCopy(salt, 0, saltWithBlockIndex, 0, salt.Length);

        using (var hashAlgorithm = PrfToManagedHmacAlgorithm(prf, password))
        {
            for (uint blockIndex = 1; numBytesRemaining > 0; blockIndex++)
            {
                // write the block index out as big-endian
                saltWithBlockIndex[saltWithBlockIndex.Length - 4] = (byte)(blockIndex >> 24);
                saltWithBlockIndex[saltWithBlockIndex.Length - 3] = (byte)(blockIndex >> 16);
                saltWithBlockIndex[saltWithBlockIndex.Length - 2] = (byte)(blockIndex >> 8);
                saltWithBlockIndex[saltWithBlockIndex.Length - 1] = (byte)blockIndex;

                // U_1 = PRF(U_0) = PRF(Salt || block_index)
                // T_blockIndex = U_1
                byte[] U_iter = hashAlgorithm.ComputeHash(saltWithBlockIndex); // this is U_1
                byte[] T_blockIndex = U_iter;

                for (int iter = 1; iter < iterationCount; iter++)
                {
                    U_iter = hashAlgorithm.ComputeHash(U_iter);
                    XorBuffers(src: U_iter, dest: T_blockIndex);
                    // At this point, the 'U_iter' variable actually contains U_{iter+1} (due to indexing differences).
                }

                // At this point, we're done iterating on this block, so copy the transformed block into retVal.
                int numBytesToCopy = Math.Min(numBytesRemaining, T_blockIndex.Length);
                Buffer.BlockCopy(T_blockIndex, 0, retVal, numBytesWritten, numBytesToCopy);
                numBytesWritten += numBytesToCopy;
                numBytesRemaining -= numBytesToCopy;
            }
        }

        // retVal := T_1 || T_2 || ... || T_n, where T_n may be truncated to meet the desired output length
        return retVal;
    }

    private static KeyedHashAlgorithm PrfToManagedHmacAlgorithm(KeyDerivationPrf prf, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            switch (prf)
            {
                case KeyDerivationPrf.HMACSHA1:
                    return new HMACSHA1(passwordBytes);
                case KeyDerivationPrf.HMACSHA256:
                    return new HMACSHA256(passwordBytes);
                case KeyDerivationPrf.HMACSHA512:
                    return new HMACSHA512(passwordBytes);
                default:
                    throw CryptoUtil.Fail("Unrecognized PRF.");
            }
        }
        finally
        {
            // The HMAC ctor makes a duplicate of this key; we clear original buffer to limit exposure to the GC.
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
        }
    }

    private static void XorBuffers(byte[] src, byte[] dest)
    {
        // Note: dest buffer is mutated.
        Debug.Assert(src.Length == dest.Length);
        for (int i = 0; i < src.Length; i++)
        {
            dest[i] ^= src[i];
        }
    }
}
