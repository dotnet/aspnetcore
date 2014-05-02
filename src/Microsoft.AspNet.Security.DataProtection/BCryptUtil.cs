// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNet.Security.DataProtection.Util;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe static class BCryptUtil
    {
        // from dpapi.h
        const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;
        const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;

        private static readonly UTF8Encoding _secureUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // constant-time buffer comparison
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool BuffersAreEqualSecure(byte* p1, byte* p2, uint count)
        {
            bool retVal = true;
            while (count-- > 0)
            {
                retVal &= (*(p1++) == *(p2++));
            }
            return retVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOverflowUnderflow(int input)
        {
            var unused = checked((uint)input);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOverflowUnderflow(uint input)
        {
            var unused = checked((int)input);
        }

        // helper function to wrap BCryptCreateHash, passing in a key used for HMAC
        public static BCryptHashHandle CreateHMACHandle(BCryptAlgorithmHandle algorithmHandle, byte* key, int keyLengthInBytes)
        {
            CheckOverflowUnderflow(keyLengthInBytes);

            BCryptHashHandle retVal;
            int status = UnsafeNativeMethods.BCryptCreateHash(algorithmHandle, out retVal, IntPtr.Zero, 0, key, (uint)keyLengthInBytes, dwFlags: 0);
            if (status != 0 || retVal == null || retVal.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            return retVal;
        }

        // helper function to wrap BCryptEncrypt; returns number of bytes written to 'output'
        // assumes the output buffer is large enough to hold the ciphertext + any necessary padding
        public static int DecryptWithPadding(BCryptKeyHandle keyHandle, byte* input, int inputLength, byte* iv, int ivLength, byte* output, int outputLength)
        {
            CheckOverflowUnderflow(inputLength);
            CheckOverflowUnderflow(ivLength);
            CheckOverflowUnderflow(outputLength);

            // BCryptEncrypt destroys the 'iv' parameter, so we need to pass a duplicate instead of the original
            if (ivLength > Constants.MAX_STACKALLOC_BYTES)
            {
                throw new InvalidOperationException();
            }
            byte* pDuplicatedIV = stackalloc byte[ivLength];
            BufferUtil.BlockCopy(from: iv, to: pDuplicatedIV, byteCount: ivLength);

            uint retVal;
            int status = UnsafeNativeMethods.BCryptDecrypt(keyHandle, input, (uint)inputLength, IntPtr.Zero, pDuplicatedIV, (uint)ivLength, output, (uint)outputLength, out retVal, BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            return checked((int)retVal);
        }

        // helper function to wrap BCryptKeyDerivation using SP800-108-CTR-HMAC-SHA512
        public static void DeriveKeysSP800108(byte[] protectedKdk, string purpose, BCryptAlgorithmHandle encryptionAlgorithmHandle, out BCryptKeyHandle encryptionKeyHandle, BCryptAlgorithmHandle hashAlgorithmHandle, out BCryptHashHandle hmacHandle, out byte[] kdfSubkey)
        {
            const int ENCRYPTION_KEY_SIZE_IN_BYTES = 256 / 8;
            const int HMAC_KEY_SIZE_IN_BYTES = 256 / 8;
            const int KDF_SUBKEY_SIZE_IN_BYTES = 512 / 8;
            const int TOTAL_NUM_BYTES_TO_DERIVE = ENCRYPTION_KEY_SIZE_IN_BYTES + HMAC_KEY_SIZE_IN_BYTES + KDF_SUBKEY_SIZE_IN_BYTES;

            // keep our buffers on the stack while we're generating key material
            byte* pBuffer = stackalloc byte[TOTAL_NUM_BYTES_TO_DERIVE]; // will be freed with frame pops
            byte* pNewEncryptionKey = pBuffer;
            byte* pNewHmacKey = &pNewEncryptionKey[ENCRYPTION_KEY_SIZE_IN_BYTES];
            byte* pNewKdfSubkey = &pNewHmacKey[HMAC_KEY_SIZE_IN_BYTES];

            protectedKdk = (byte[])protectedKdk.Clone(); // CryptUnprotectMemory mutates its input, so we preserve the original
            fixed (byte* pKdk = protectedKdk)
            {
                try
                {
                    // Since the KDK is pinned, the GC won't move around the array containing the plaintext key before we
                    // have the opportunity to clear its contents.
                    UnprotectMemoryWithinThisProcess(pKdk, (uint)protectedKdk.Length);

                    byte[] purposeBytes = (!String.IsNullOrEmpty(purpose)) ? _secureUtf8Encoding.GetBytes(purpose) : null;
                    SP800_108Helper.DeriveKeys(pKdk, protectedKdk.Length, purposeBytes, pBuffer, TOTAL_NUM_BYTES_TO_DERIVE);

                    // Split into AES, HMAC, and KDF subkeys
                    encryptionKeyHandle = ImportKey(encryptionAlgorithmHandle, pNewEncryptionKey, ENCRYPTION_KEY_SIZE_IN_BYTES);
                    hmacHandle = CreateHMACHandle(hashAlgorithmHandle, pNewHmacKey, HMAC_KEY_SIZE_IN_BYTES);
                    kdfSubkey = BufferUtil.ToProtectedManagedByteArray(pNewKdfSubkey, KDF_SUBKEY_SIZE_IN_BYTES);
                }
                finally
                {
                    BufferUtil.SecureZeroMemory(pKdk, protectedKdk.Length);
                }
            }
        }

        // helper function to wrap BCryptDuplicateHash
        public static BCryptHashHandle DuplicateHash(BCryptHashHandle hashHandle)
        {
            BCryptHashHandle retVal;
            int status = UnsafeNativeMethods.BCryptDuplicateHash(hashHandle, out retVal, IntPtr.Zero, 0, dwFlags: 0);
            if (status != 0 || retVal == null || retVal.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            return retVal;
        }

        // helper function to wrap BCryptEncrypt; returns number of bytes written to 'output'
        // assumes the output buffer is large enough to hold the ciphertext + any necessary padding
        public static int EncryptWithPadding(BCryptKeyHandle keyHandle, byte* input, int inputLength, byte* iv, int ivLength, byte* output, int outputLength)
        {
            CheckOverflowUnderflow(inputLength);
            CheckOverflowUnderflow(ivLength);
            CheckOverflowUnderflow(outputLength);

            // BCryptEncrypt destroys the 'iv' parameter, so we need to pass a duplicate instead of the original
            if (ivLength > Constants.MAX_STACKALLOC_BYTES)
            {
                throw new InvalidOperationException();
            }
            byte* pDuplicatedIV = stackalloc byte[ivLength];
            BufferUtil.BlockCopy(from: iv, to: pDuplicatedIV, byteCount: ivLength);

            uint retVal;
            int status = UnsafeNativeMethods.BCryptEncrypt(keyHandle, input, (uint)inputLength, IntPtr.Zero, pDuplicatedIV, (uint)ivLength, output, (uint)outputLength, out retVal, BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            return checked((int)retVal);
        }

        // helper function to take a key, apply a purpose, and generate a new subkey ("entropy") for DPAPI-specific scenarios
        public static byte[] GenerateDpapiSubkey(byte[] previousKey, string purpose)
        {
            Debug.Assert(previousKey != null);
            purpose = purpose ?? String.Empty; // cannot be null

            // create the HMAC object
            BCryptHashHandle hashHandle;
            fixed (byte* pPreviousKey = previousKey)
            {
                hashHandle = CreateHMACHandle(Algorithms.HMACSHA256AlgorithmHandle, pPreviousKey, previousKey.Length);
            }

            // hash the purpose string, treating it as UTF-16LE
            using (hashHandle)
            {
                byte[] retVal = new byte[256 / 8]; // fixed length output since we're hardcoded to HMACSHA256
                fixed (byte* pRetVal = retVal)
                {
                    fixed (char* pPurpose = purpose)
                    {
                        HashData(hashHandle, (byte*)pPurpose, checked(purpose.Length * sizeof(char)), pRetVal, retVal.Length);
                        return retVal;
                    }
                }
            }
        }

        // helper function that's similar to RNGCryptoServiceProvider, but works directly with pointers
        public static void GenRandom(byte* buffer, int bufferBytes)
        {
            CheckOverflowUnderflow(bufferBytes);

            int status = UnsafeNativeMethods.BCryptGenRandom(IntPtr.Zero, buffer, (uint)bufferBytes, BCryptGenRandomFlags.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }
        }

        // helper function that wraps BCryptHashData / BCryptFinishHash
        public static void HashData(BCryptHashHandle hashHandle, byte* input, int inputBytes, byte* output, int outputBytes)
        {
            CheckOverflowUnderflow(inputBytes);
            CheckOverflowUnderflow(outputBytes);

            int status = UnsafeNativeMethods.BCryptHashData(hashHandle, input, (uint)inputBytes, dwFlags: 0);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            status = UnsafeNativeMethods.BCryptFinishHash(hashHandle, output, (uint)outputBytes, dwFlags: 0);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }
        }

        // helper function that wraps BCryptImportKey with a key data blob
        public static BCryptKeyHandle ImportKey(BCryptAlgorithmHandle algHandle, byte* key, int keyBytes)
        {
            CheckOverflowUnderflow(keyBytes);

            byte[] heapAllocatedKeyDataBlob = null;
            int numBytesRequiredForKeyDataBlob = checked(keyBytes + sizeof(BCRYPT_KEY_DATA_BLOB_HEADER));
            if (numBytesRequiredForKeyDataBlob > Constants.MAX_STACKALLOC_BYTES)
            {
                heapAllocatedKeyDataBlob = new byte[numBytesRequiredForKeyDataBlob]; // allocate on heap if we cannot allocate on stack
            }

            int status;
            BCryptKeyHandle retVal;
            fixed (byte* pHeapAllocatedKeyDataBlob = heapAllocatedKeyDataBlob)
            {
                // The header is first; if it wasn't heap-allocated we can stack-allocate now
                BCRYPT_KEY_DATA_BLOB_HEADER* pKeyDataBlobHeader = (BCRYPT_KEY_DATA_BLOB_HEADER*)pHeapAllocatedKeyDataBlob;
                if (pKeyDataBlobHeader == null)
                {
                    byte* temp = stackalloc byte[numBytesRequiredForKeyDataBlob]; // won't be released until frame pops
                    pKeyDataBlobHeader = (BCRYPT_KEY_DATA_BLOB_HEADER*)temp;
                }
                BCRYPT_KEY_DATA_BLOB_HEADER.Initialize(ref *pKeyDataBlobHeader);
                pKeyDataBlobHeader->cbKeyData = (uint)keyBytes;

                // the raw material immediately follows the header
                byte* pKeyDataRawMaterial = (byte*)(&pKeyDataBlobHeader[1]);

                try
                {
                    BufferUtil.BlockCopy(from: key, to: pKeyDataRawMaterial, byteCount: keyBytes);
                    status = UnsafeNativeMethods.BCryptImportKey(algHandle, IntPtr.Zero, Constants.BCRYPT_KEY_DATA_BLOB, out retVal, IntPtr.Zero, 0, (byte*)pKeyDataBlobHeader, (uint)numBytesRequiredForKeyDataBlob, dwFlags: 0);
                }
                finally
                {
                    // zero out the key we just copied
                    BufferUtil.SecureZeroMemory(pKeyDataRawMaterial, keyBytes);
                }
            }

            if (status != 0 || retVal == null || retVal.IsInvalid)
            {
                throw new CryptographicException(status);
            }
            return retVal;
        }

        internal static void ProtectMemoryWithinThisProcess(byte* pBuffer, uint bufferLength)
        {
            Debug.Assert(pBuffer != null);
            Debug.Assert(bufferLength % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0, "Input buffer size must be a multiple of CRYPTPROTECTMEMORY_BLOCK_SIZE.");

            bool success = UnsafeNativeMethods.CryptProtectMemory(pBuffer, bufferLength, CRYPTPROTECTMEMORY_SAME_PROCESS);
            if (!success)
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }
        }

        internal static void UnprotectMemoryWithinThisProcess(byte* pBuffer, uint bufferLength)
        {
            Debug.Assert(pBuffer != null);
            Debug.Assert(bufferLength % CRYPTPROTECTMEMORY_BLOCK_SIZE == 0, "Input buffer size must be a multiple of CRYPTPROTECTMEMORY_BLOCK_SIZE.");

            bool success = UnsafeNativeMethods.CryptUnprotectMemory(pBuffer, bufferLength, CRYPTPROTECTMEMORY_SAME_PROCESS);
            if (!success)
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }
        }
    }
}
