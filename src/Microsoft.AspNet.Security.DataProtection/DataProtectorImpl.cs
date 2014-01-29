using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Resources;
using Microsoft.AspNet.Security.DataProtection.Util;

namespace Microsoft.AspNet.Security.DataProtection {
    internal unsafe sealed class DataProtectorImpl : IDataProtector {

        private const int AES_BLOCK_LENGTH_IN_BYTES = 128 / 8;
        private const int MAC_LENGTH_IN_BYTES = 256 / 8;

        private readonly BCryptKeyHandle _aesKeyHandle;
        private readonly BCryptHashHandle _hmacHashHandle;
        private readonly BCryptKeyHandle _kdfSubkeyHandle;

        public DataProtectorImpl(BCryptKeyHandle aesKeyHandle, BCryptHashHandle hmacHashHandle, BCryptKeyHandle kdfSubkeyHandle) {
            _aesKeyHandle = aesKeyHandle;
            _hmacHashHandle = hmacHashHandle;
            _kdfSubkeyHandle = kdfSubkeyHandle;
        }

        private static int CalculateTotalProtectedDataSize(int unprotectedDataSize) {
            Debug.Assert(unprotectedDataSize >= 0);

            // Calculates 
            int numFullBlocks = unprotectedDataSize / AES_BLOCK_LENGTH_IN_BYTES;
            return checked(AES_BLOCK_LENGTH_IN_BYTES /* IV */ + (numFullBlocks + 1) * AES_BLOCK_LENGTH_IN_BYTES /* ciphertext w/ padding */ + MAC_LENGTH_IN_BYTES /* HMAC */);
        }

        private static CryptographicException CreateGenericCryptographicException() {
            return new CryptographicException(Res.DataProtectorImpl_BadEncryptedData);
        }

        public IDataProtector CreateSubProtector(string purpose) {
            BCryptKeyHandle newAesKeyHandle;
            BCryptHashHandle newHmacHashHandle;
            BCryptKeyHandle newKdfSubkeyHandle;

            BCryptUtil.DeriveKeysSP800108(Algorithms.SP800108AlgorithmHandle, _kdfSubkeyHandle, purpose, Algorithms.AESAlgorithmHandle, out newAesKeyHandle, Algorithms.HMACSHA256AlgorithmHandle, out newHmacHashHandle, out newKdfSubkeyHandle);
            return new DataProtectorImpl(newAesKeyHandle, newHmacHashHandle, newKdfSubkeyHandle);
        }

        public void Dispose() {
            _aesKeyHandle.Dispose();
            _hmacHashHandle.Dispose();
            _kdfSubkeyHandle.Dispose();
        }

        public byte[] Protect(byte[] unprotectedData) {
            if (unprotectedData == null) {
                throw new ArgumentNullException("unprotectedData");
            }

            // When this method finishes, protectedData will contain { IV || ciphertext || HMAC(IV || ciphertext) }
            byte[] protectedData = new byte[CalculateTotalProtectedDataSize(unprotectedData.Length)];

            fixed (byte* pProtectedData = protectedData) {
                // first, generate a random IV for CBC mode encryption
                byte* pIV = pProtectedData;
                BCryptUtil.GenRandom(pIV, AES_BLOCK_LENGTH_IN_BYTES);

                // then, encrypt the plaintext contents
                byte* pCiphertext = &pIV[AES_BLOCK_LENGTH_IN_BYTES];
                int expectedCiphertextLength = protectedData.Length - AES_BLOCK_LENGTH_IN_BYTES - MAC_LENGTH_IN_BYTES;
                fixed (byte* pPlaintext = unprotectedData) {
                    int actualCiphertextLength = BCryptUtil.EncryptWithPadding(_aesKeyHandle, pPlaintext, unprotectedData.Length, pIV, AES_BLOCK_LENGTH_IN_BYTES, pCiphertext, expectedCiphertextLength);
                    if (actualCiphertextLength != expectedCiphertextLength) {
                        throw new InvalidOperationException("Unexpected error while encrypting data.");
                    }
                }

                // finally, calculate an HMAC over { IV || ciphertext }
                byte* pMac = &pCiphertext[expectedCiphertextLength];
                using (var clonedHashHandle = BCryptUtil.DuplicateHash(_hmacHashHandle)) {
                    // Use a cloned hash handle since IDataProtector instances could be singletons, but BCryptHashHandle instances contain
                    // state hence aren't thread-safe. Our own perf testing shows that duplicating existing hash handles is very fast.
                    BCryptUtil.HashData(clonedHashHandle, pProtectedData, AES_BLOCK_LENGTH_IN_BYTES + expectedCiphertextLength, pMac, MAC_LENGTH_IN_BYTES);
                }
            }

            return protectedData;
        }

        public byte[] Unprotect(byte[] protectedData) {
            if (protectedData == null) {
                throw new ArgumentNullException("protectedData");
            }

            byte[] retVal = null;
            try {
                retVal = UnprotectImpl(protectedData);
            }
            catch {
                // swallow all exceptions; we'll homogenize
            }

            if (retVal != null) {
                return retVal;
            }
            else {
                throw CreateGenericCryptographicException();
            }
        }

        private byte[] UnprotectImpl(byte[] protectedData) {
            Debug.Assert(protectedData != null);

            // is the protected data even long enough to be valid?
            if (protectedData.Length < AES_BLOCK_LENGTH_IN_BYTES /* IV */ + AES_BLOCK_LENGTH_IN_BYTES /* min ciphertext size = 1 block */ + MAC_LENGTH_IN_BYTES) {
                return null;
            }

            fixed (byte* pProtectedData = protectedData) {
                // calculate pointer offsets
                byte* pIV = pProtectedData;
                byte* pCiphertext = &pProtectedData[AES_BLOCK_LENGTH_IN_BYTES];
                int ciphertextLength = protectedData.Length - AES_BLOCK_LENGTH_IN_BYTES /* IV */ - MAC_LENGTH_IN_BYTES /* MAC */;
                byte* pSuppliedMac = &pCiphertext[ciphertextLength];

                // first, ensure that the MAC is valid
                byte* pCalculatedMac = stackalloc byte[MAC_LENGTH_IN_BYTES];
                using (var clonedHashHandle = BCryptUtil.DuplicateHash(_hmacHashHandle)) {
                    // see comments in Protect(byte[]) for why we duplicate the hash
                    BCryptUtil.HashData(clonedHashHandle, pProtectedData, AES_BLOCK_LENGTH_IN_BYTES + ciphertextLength, pCalculatedMac, MAC_LENGTH_IN_BYTES);
                }
                if (!BCryptUtil.BuffersAreEqualSecure(pSuppliedMac, pCalculatedMac, MAC_LENGTH_IN_BYTES)) {
                    return null; // MAC check failed
                }

                // next, perform the actual decryption
                // we don't know the actual plaintext length, but we know it must be strictly less than the ciphertext length
                int plaintextBufferLength = ciphertextLength;
                byte[] heapAllocatedPlaintext = null;
                if (ciphertextLength > Constants.MAX_STACKALLOC_BYTES) {
                    heapAllocatedPlaintext = new byte[plaintextBufferLength];
                }

                fixed (byte* pHeapAllocatedPlaintext = heapAllocatedPlaintext) {
                    byte* pPlaintextBuffer = pHeapAllocatedPlaintext;
                    if (pPlaintextBuffer == null) {
                        byte* temp = stackalloc byte[plaintextBufferLength]; // will be released when frame pops
                        pPlaintextBuffer = temp;
                    }

                    int actualPlaintextLength = BCryptUtil.DecryptWithPadding(_aesKeyHandle, pCiphertext, ciphertextLength, pIV, AES_BLOCK_LENGTH_IN_BYTES, pPlaintextBuffer, plaintextBufferLength);
                    Debug.Assert(actualPlaintextLength >= 0 && actualPlaintextLength < ciphertextLength);

                    // truncate the return value to accomodate the plaintext size perfectly
                    byte[] retVal = new byte[actualPlaintextLength];
                    fixed (byte* pRetVal = retVal) {
                        BufferUtil.BlockCopy(from: (IntPtr)pPlaintextBuffer, to: (IntPtr)pRetVal, byteCount: actualPlaintextLength);
                    }
                    return retVal;
                }
            }
        }

    }
}
