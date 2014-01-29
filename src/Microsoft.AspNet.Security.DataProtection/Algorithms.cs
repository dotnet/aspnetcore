using System;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal static unsafe class Algorithms
    {
        public static readonly BCryptAlgorithmHandle AESAlgorithmHandle = CreateAESAlgorithmHandle();
        public static readonly BCryptAlgorithmHandle HMACSHA256AlgorithmHandle = CreateHMACSHA256AlgorithmHandle();
        public static readonly BCryptAlgorithmHandle HMACSHA512AlgorithmHandle = CreateHMACSHA512AlgorithmHandle();
        public static readonly BCryptAlgorithmHandle SP800108AlgorithmHandle = CreateSP800108AlgorithmHandle();

        private static BCryptAlgorithmHandle CreateAESAlgorithmHandle()
        {
            // create the AES instance
            BCryptAlgorithmHandle algHandle;
            int status = UnsafeNativeMethods.BCryptOpenAlgorithmProvider(out algHandle, Constants.BCRYPT_AES_ALGORITHM, Constants.MS_PRIMITIVE_PROVIDER, dwFlags: 0);
            if (status != 0 || algHandle == null || algHandle.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            // change it to use CBC chaining; it already uses PKCS7 padding by default
            fixed (char* pCbcMode = Constants.BCRYPT_CHAIN_MODE_CBC)
            {
                status = UnsafeNativeMethods.BCryptSetProperty(algHandle, Constants.BCRYPT_CHAINING_MODE, (IntPtr) pCbcMode, (uint) ((Constants.BCRYPT_CHAIN_MODE_CBC.Length + 1 /* trailing null */)*sizeof (char)), dwFlags: 0);
            }
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            return algHandle;
        }

        private static BCryptAlgorithmHandle CreateHMACSHA256AlgorithmHandle()
        {
            // create the HMACSHA-256 instance
            BCryptAlgorithmHandle algHandle;
            int status = UnsafeNativeMethods.BCryptOpenAlgorithmProvider(out algHandle, Constants.BCRYPT_SHA256_ALGORITHM, Constants.MS_PRIMITIVE_PROVIDER, dwFlags: BCryptAlgorithmFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
            if (status != 0 || algHandle == null || algHandle.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            return algHandle;
        }

        private static BCryptAlgorithmHandle CreateHMACSHA512AlgorithmHandle()
        {
            // create the HMACSHA-512 instance
            BCryptAlgorithmHandle algHandle;
            int status = UnsafeNativeMethods.BCryptOpenAlgorithmProvider(out algHandle, Constants.BCRYPT_SHA512_ALGORITHM, Constants.MS_PRIMITIVE_PROVIDER, dwFlags: BCryptAlgorithmFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
            if (status != 0 || algHandle == null || algHandle.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            return algHandle;
        }

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
    }
}