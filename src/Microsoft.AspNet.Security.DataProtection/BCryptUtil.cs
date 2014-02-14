using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Util;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal static unsafe class BCryptUtil
    {
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
            var unused = checked((uint) input);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckOverflowUnderflow(uint input)
        {
            var unused = checked((int) input);
        }

        // helper function to wrap BCryptCreateHash
        public static BCryptHashHandle CreateHash(BCryptAlgorithmHandle algorithmHandle, byte* key, int keyLengthInBytes)
        {
            CheckOverflowUnderflow(keyLengthInBytes);

            BCryptHashHandle retVal;
            int status = UnsafeNativeMethods.BCryptCreateHash(algorithmHandle, out retVal, IntPtr.Zero, 0, key, (uint) keyLengthInBytes, dwFlags: 0);
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
            BufferUtil.BlockCopy(from: (IntPtr) iv, to: (IntPtr) pDuplicatedIV, byteCount: ivLength);

            uint retVal;
            int status = UnsafeNativeMethods.BCryptDecrypt(keyHandle, input, (uint) inputLength, IntPtr.Zero, pDuplicatedIV, (uint) ivLength, output, (uint) outputLength, out retVal, BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            return checked((int) retVal);
        }

        // helper function to wrap BCryptKeyDerivation using SP800-108-CTR-HMAC-SHA512
        public static void DeriveKeysSP800108(BCryptAlgorithmHandle kdfAlgorithmHandle, BCryptKeyHandle keyHandle, string purpose, BCryptAlgorithmHandle encryptionAlgorithmHandle, out BCryptKeyHandle encryptionKeyHandle, BCryptAlgorithmHandle hashAlgorithmHandle, out BCryptHashHandle hmacHandle, out BCryptKeyHandle kdfKeyHandle)
        {
            const int ENCRYPTION_KEY_SIZE_IN_BYTES = 256/8;
            const int HMAC_KEY_SIZE_IN_BYTES = 256/8;
            const int KDF_SUBKEY_SIZE_IN_BYTES = 512/8;
            const int TOTAL_NUM_BYTES_TO_DERIVE = ENCRYPTION_KEY_SIZE_IN_BYTES + HMAC_KEY_SIZE_IN_BYTES + KDF_SUBKEY_SIZE_IN_BYTES;

            // keep our buffers on the stack while we're generating key material
            byte* pBuffer = stackalloc byte[TOTAL_NUM_BYTES_TO_DERIVE]; // will be freed with frame pops
            byte* pNewEncryptionKey = pBuffer;
            byte* pNewHmacKey = &pNewEncryptionKey[ENCRYPTION_KEY_SIZE_IN_BYTES];
            byte* pNewKdfSubkey = &pNewHmacKey[HMAC_KEY_SIZE_IN_BYTES];

            try
            {
                fixed (char* pszPrfAlgorithmName = Constants.BCRYPT_SHA512_ALGORITHM)
                {
                    // Create a buffer to hold the hash algorithm name, currently hardcoded to HMACSHA512
                    uint numBuffers = 1;
                    BCryptBuffer* pBCryptBuffers = stackalloc BCryptBuffer[2];
                    pBCryptBuffers[0].BufferType = BCryptKeyDerivationBufferType.KDF_HASH_ALGORITHM;
                    pBCryptBuffers[0].pvBuffer = (IntPtr) pszPrfAlgorithmName;
                    pBCryptBuffers[0].cbBuffer = (uint) ((Constants.BCRYPT_SHA512_ALGORITHM.Length + 1)*sizeof (char)); // per http://msdn.microsoft.com/en-us/library/windows/desktop/aa375368(v=vs.85).aspx, need to include terminating null
                    fixed (char* pszPurpose = (String.IsNullOrEmpty(purpose) ? (string) null : purpose))
                    {
                        // Create a buffer to hold the purpose string if it is specified (we'll treat it as UTF-16LE)
                        if (pszPurpose != null)
                        {
                            numBuffers = 2;
                            pBCryptBuffers[1].BufferType = BCryptKeyDerivationBufferType.KDF_LABEL;
                            pBCryptBuffers[1].pvBuffer = (IntPtr) pszPurpose;
                            pBCryptBuffers[1].cbBuffer = checked((uint) (purpose.Length*sizeof (char)));
                        }

                        // .. and the header ..
                        BCryptBufferDesc bufferDesc = default(BCryptBufferDesc);
                        BCryptBufferDesc.Initialize(ref bufferDesc);
                        bufferDesc.cBuffers = numBuffers;
                        bufferDesc.pBuffers = pBCryptBuffers;

                        uint numBytesDerived;
                        int status = UnsafeNativeMethods.BCryptKeyDerivation(keyHandle, &bufferDesc, pBuffer, TOTAL_NUM_BYTES_TO_DERIVE, out numBytesDerived, dwFlags: 0);
                        if (status != 0 || numBytesDerived != TOTAL_NUM_BYTES_TO_DERIVE)
                        {
                            throw new CryptographicException(status);
                        }
                    }
                }

                // At this point, we have all the bytes we need.
                encryptionKeyHandle = ImportKey(encryptionAlgorithmHandle, pNewEncryptionKey, ENCRYPTION_KEY_SIZE_IN_BYTES);
                hmacHandle = CreateHash(hashAlgorithmHandle, pNewHmacKey, HMAC_KEY_SIZE_IN_BYTES);
                kdfKeyHandle = ImportKey(kdfAlgorithmHandle, pNewKdfSubkey, KDF_SUBKEY_SIZE_IN_BYTES);
            }
            finally
            {
                BufferUtil.ZeroMemory(pBuffer, TOTAL_NUM_BYTES_TO_DERIVE);
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
            BufferUtil.BlockCopy(from: (IntPtr) iv, to: (IntPtr) pDuplicatedIV, byteCount: ivLength);

            uint retVal;
            int status = UnsafeNativeMethods.BCryptEncrypt(keyHandle, input, (uint) inputLength, IntPtr.Zero, pDuplicatedIV, (uint) ivLength, output, (uint) outputLength, out retVal, BCryptEncryptFlags.BCRYPT_BLOCK_PADDING);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            return checked((int) retVal);
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
                hashHandle = CreateHash(Algorithms.HMACSHA256AlgorithmHandle, pPreviousKey, previousKey.Length);
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

            int status = UnsafeNativeMethods.BCryptGenRandom(IntPtr.Zero, buffer, (uint) bufferBytes, BCryptGenRandomFlags.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
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

            int status = UnsafeNativeMethods.BCryptHashData(hashHandle, input, (uint) inputBytes, dwFlags: 0);
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            status = UnsafeNativeMethods.BCryptFinishHash(hashHandle, output, (uint) outputBytes, dwFlags: 0);
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
            int numBytesRequiredForKeyDataBlob = checked(keyBytes + sizeof (BCRYPT_KEY_DATA_BLOB_HEADER));
            if (numBytesRequiredForKeyDataBlob > Constants.MAX_STACKALLOC_BYTES)
            {
                heapAllocatedKeyDataBlob = new byte[numBytesRequiredForKeyDataBlob]; // allocate on heap if we cannot allocate on stack
            }

            int status;
            BCryptKeyHandle retVal;
            fixed (byte* pHeapAllocatedKeyDataBlob = heapAllocatedKeyDataBlob)
            {
                // The header is first
                BCRYPT_KEY_DATA_BLOB_HEADER* pKeyDataBlobHeader = (BCRYPT_KEY_DATA_BLOB_HEADER*) pHeapAllocatedKeyDataBlob;
                if (pKeyDataBlobHeader == null)
                {
                    byte* temp = stackalloc byte[numBytesRequiredForKeyDataBlob]; // won't be released until frame pops
                    pKeyDataBlobHeader = (BCRYPT_KEY_DATA_BLOB_HEADER*) temp;
                }
                BCRYPT_KEY_DATA_BLOB_HEADER.Initialize(ref *pKeyDataBlobHeader);
                pKeyDataBlobHeader->cbKeyData = (uint) keyBytes;

                // the raw material immediately follows the header
                byte* pKeyDataRawMaterial = (byte*) (&pKeyDataBlobHeader[1]);

                try
                {
                    BufferUtil.BlockCopy(from: (IntPtr) key, to: (IntPtr) pKeyDataRawMaterial, byteCount: keyBytes);
                    status = UnsafeNativeMethods.BCryptImportKey(algHandle, IntPtr.Zero, Constants.BCRYPT_KEY_DATA_BLOB, out retVal, IntPtr.Zero, 0, (byte*) pKeyDataBlobHeader, (uint) numBytesRequiredForKeyDataBlob, dwFlags: 0);
                }
                finally
                {
                    // zero out the key we just copied
                    BufferUtil.ZeroMemory(pKeyDataRawMaterial, keyBytes);
                }
            }

            if (status != 0 || retVal == null || retVal.IsInvalid)
            {
                throw new CryptographicException(status);
            }
            return retVal;
        }
    }
}