using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Util;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// Provides an implementation of the SP800-108-CTR-HMACSHA512 key derivation function.
    /// This class assumes at least Windows 7 / Server 2008 R2.
    /// </summary>
    /// <remarks>
    /// More info at http://csrc.nist.gov/publications/nistpubs/800-108/sp800-108.pdf, Sec. 5.1.
    /// </remarks>
    internal unsafe static class SP800_108Helper
    {
        private const string BCRYPT_LIB = "bcrypt.dll";

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/hh448506(v=vs.85).aspx
        private delegate int BCryptKeyDerivation(
            [In] BCryptKeyHandle hKey,
            [In] BCryptBufferDesc* pParameterList,
            [In] byte* pbDerivedKey,
            [In] uint cbDerivedKey,
            [Out] out uint pcbResult,
            [In] uint dwFlags);

        private static readonly BCryptAlgorithmHandle SP800108AlgorithmHandle;
        private delegate void DeriveKeysDelegate(byte* pKdk, int kdkByteLength, byte[] purpose, byte* pOutputBuffer, uint outputBufferByteLength);
        private static DeriveKeysDelegate _thunk = CreateThunk(out SP800108AlgorithmHandle);

        private static BCryptAlgorithmHandle CreateSP800108AlgorithmHandle()
        {
            // create the SP800-108 instance
            BCryptAlgorithmHandle algHandle;
            int status = UnsafeNativeMethods.BCryptOpenAlgorithmProvider(out algHandle, Constants.BCRYPT_SP800108_CTR_HMAC_ALGORITHM, Constants.MS_PRIMITIVE_PROVIDER, dwFlags: 0);
            if (status != 0 || algHandle == null || algHandle.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            return algHandle;
        }

        private static DeriveKeysDelegate CreateThunk(out BCryptAlgorithmHandle sp800108AlgorithmHandle)
        {
            SafeLibraryHandle bcryptLibHandle = SafeLibraryHandle.Open(BCRYPT_LIB);
            var win8Thunk = bcryptLibHandle.GetProcAddress<BCryptKeyDerivation>("BCryptKeyDerivation", throwIfNotFound: false);
            if (win8Thunk != null)
            {
                // Permanently reference bcrypt.dll for the lifetime of the AppDomain.
                // When the AD goes away the SafeLibraryHandle will automatically be released.
                GCHandle.Alloc(bcryptLibHandle);
                sp800108AlgorithmHandle = CreateSP800108AlgorithmHandle();
                return win8Thunk.DeriveKeysWin8;
            }
            else
            {
                sp800108AlgorithmHandle = null;
                return DeriveKeysWin7;
            }
        }

        /// <summary>
        /// Performs a key derivation using SP800-108-CTR-HMACSHA512.
        /// </summary>
        /// <param name="pKdk">Pointer to the key derivation key.</param>
        /// <param name="kdkByteLength">Length (in bytes) of the key derivation key.</param>
        /// <param name="purpose">Purpose to attach to the generated subkey. Corresponds to the 'Label' parameter
        /// in the KDF. May be null.</param>
        /// <param name="pOutputBuffer">Pointer to a buffer which will receive the subkey.</param>
        /// <param name="outputBufferByteLength">Length (in bytes) of the output buffer.</param>
        public static void DeriveKeys(byte* pKdk, int kdkByteLength, byte[] purpose, byte* pOutputBuffer, uint outputBufferByteLength)
        {
            _thunk(pKdk, kdkByteLength, purpose, pOutputBuffer, outputBufferByteLength);
        }

        // Wraps our own SP800-108 implementation around bcrypt.dll primitives.
        private static void DeriveKeysWin7(byte* pKdk, int kdkByteLength, byte[] purpose, byte* pOutputBuffer, uint outputBufferByteLength)
        {
            const int TEMP_RESULT_OUTPUT_BYTES = 512 / 8; // hardcoded to HMACSHA512

            // NOTE: pOutputBuffer and outputBufferByteLength are modified as data is copied from temporary buffers
            // to the final output buffer.

            // used to hold the output of the HMACSHA512 routine
            byte* pTempResultBuffer = stackalloc byte[TEMP_RESULT_OUTPUT_BYTES];
            int purposeLength = (purpose != null) ? purpose.Length : 0;

            // this will be zero-inited
            byte[] dataToBeHashed = new byte[checked(
                sizeof(int) /* [i] */
                + purposeLength /* Label */
                + 1 /* 0x00 */
                + 0 /* Context */
                + sizeof(int) /* [L] */)];

            fixed (byte* pDataToBeHashed = dataToBeHashed)
            {
                // Step 1: copy purpose into Label part of data to be hashed
                if (purposeLength > 0)
                {
                    fixed (byte* pPurpose = purpose)
                    {
                        BufferUtil.BlockCopy(from: pPurpose, to: &pDataToBeHashed[sizeof(int)], byteCount: purposeLength);
                    }
                }

                // Step 2: copy [L] into last part of data to be hashed, big-endian
                uint numBitsToGenerate = checked(outputBufferByteLength * 8);
                MemoryUtil.UnalignedWriteBigEndian(&pDataToBeHashed[dataToBeHashed.Length - sizeof(int)], numBitsToGenerate);

                // Step 3: iterate until all desired bytes have been generated
                for (int i = 1; outputBufferByteLength > 0; i++)
                {
                    // Step 3a: Copy [i] into the first part of data to be hashed, big-endian
                    MemoryUtil.UnalignedWriteBigEndian(pDataToBeHashed, (uint)i);

                    // Step 3b: Hash. Win7 doesn't allow reusing hash algorithm objects after the final hash
                    // has been computed, so we need to create a new instance of the hash object for each
                    // iteration. We don't bother with this optimization on Win8 since we call BCryptKeyDerivation
                    // instead when on that OS.
                    using (var hashHandle = BCryptUtil.CreateHMACHandle(Algorithms.HMACSHA512AlgorithmHandle, pKdk, kdkByteLength))
                    {
                        BCryptUtil.HashData(hashHandle, pDataToBeHashed, dataToBeHashed.Length, pTempResultBuffer, TEMP_RESULT_OUTPUT_BYTES);
                    }

                    // Step 3c: Copy bytes from the temporary buffer to the output buffer.
                    uint numBytesToCopy = Math.Min(outputBufferByteLength, (uint)TEMP_RESULT_OUTPUT_BYTES);
                    BufferUtil.BlockCopy(from: pTempResultBuffer, to: pOutputBuffer, byteCount: numBytesToCopy);
                    pOutputBuffer += numBytesToCopy;
                    outputBufferByteLength -= numBytesToCopy;
                }
            }
        }

        // Calls into the Win8 implementation (bcrypt.dll) for the SP800-108 KDF
        private static void DeriveKeysWin8(this BCryptKeyDerivation fnKeyDerivation, byte* pKdk, int kdkByteLength, byte[] purpose, byte* pOutputBuffer, uint outputBufferByteLength)
        {
            // Create a buffer to hold the hash algorithm name
            fixed (char* pszPrfAlgorithmName = Constants.BCRYPT_SHA512_ALGORITHM)
            {
                BCryptBuffer* pBCryptBuffers = stackalloc BCryptBuffer[2];

                // The first buffer should contain the PRF algorithm name (hardcoded to HMACSHA512).
                // Per http://msdn.microsoft.com/en-us/library/aa375368(v=vs.85).aspx, cbBuffer must include the terminating null char.
                pBCryptBuffers[0].BufferType = BCryptKeyDerivationBufferType.KDF_HASH_ALGORITHM;
                pBCryptBuffers[0].pvBuffer = (IntPtr)pszPrfAlgorithmName;
                pBCryptBuffers[0].cbBuffer = (uint)((Constants.BCRYPT_SHA512_ALGORITHM.Length + 1) * sizeof(char));
                uint numBuffers = 1;

                fixed (byte* pPurpose = ((purpose != null && purpose.Length != 0) ? purpose : null))
                {
                    if (pPurpose != null)
                    {
                        // The second buffer will hold the purpose bytes if they're specified.
                        pBCryptBuffers[1].BufferType = BCryptKeyDerivationBufferType.KDF_LABEL;
                        pBCryptBuffers[1].pvBuffer = (IntPtr)pPurpose;
                        pBCryptBuffers[1].cbBuffer = (uint)purpose.Length;
                        numBuffers = 2;
                    }

                    // Add the header
                    BCryptBufferDesc bufferDesc = default(BCryptBufferDesc);
                    BCryptBufferDesc.Initialize(ref bufferDesc);
                    bufferDesc.cBuffers = numBuffers;
                    bufferDesc.pBuffers = pBCryptBuffers;

                    // Finally, perform the calculation and validate that the actual number of bytes derived matches
                    // the number that the caller requested.
                    uint numBytesDerived;
                    int status;
                    using (BCryptKeyHandle kdkHandle = BCryptUtil.ImportKey(SP800108AlgorithmHandle, pKdk, kdkByteLength))
                    {
                        status = fnKeyDerivation(kdkHandle, &bufferDesc, pOutputBuffer, outputBufferByteLength, out numBytesDerived, dwFlags: 0);
                    }
                    if (status != 0 || numBytesDerived != outputBufferByteLength)
                    {
                        throw new CryptographicException(status);
                    }
                }
            }
        }
    }
}
