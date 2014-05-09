// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe static class Algorithms
    {
        public static readonly BCryptAlgorithmHandle AESAlgorithmHandle = CreateAESAlgorithmHandle();
        public static readonly BCryptAlgorithmHandle HMACSHA256AlgorithmHandle = CreateHMACSHA256AlgorithmHandle();
        public static readonly BCryptAlgorithmHandle HMACSHA512AlgorithmHandle = CreateHMACSHA512AlgorithmHandle();

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
                status = UnsafeNativeMethods.BCryptSetProperty(algHandle, Constants.BCRYPT_CHAINING_MODE, (IntPtr)pCbcMode, (uint)((Constants.BCRYPT_CHAIN_MODE_CBC.Length + 1 /* trailing null */) * sizeof(char)), dwFlags: 0);
            }
            if (status != 0)
            {
                throw new CryptographicException(status);
            }

            return algHandle;
        }

        internal static BCryptAlgorithmHandle CreateGenericHMACHandleFromPrimitiveProvider(string algorithmName)
        {
            BCryptAlgorithmHandle algHandle;
            int status = UnsafeNativeMethods.BCryptOpenAlgorithmProvider(out algHandle, algorithmName, Constants.MS_PRIMITIVE_PROVIDER, dwFlags: BCryptAlgorithmFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
            if (status != 0 || algHandle == null || algHandle.IsInvalid)
            {
                throw new CryptographicException(status);
            }

            return algHandle;
        }

        private static BCryptAlgorithmHandle CreateHMACSHA256AlgorithmHandle()
        {
            // create the HMACSHA-256 instance
            return CreateGenericHMACHandleFromPrimitiveProvider(Constants.BCRYPT_SHA256_ALGORITHM);
        }

        private static BCryptAlgorithmHandle CreateHMACSHA512AlgorithmHandle()
        {
            // create the HMACSHA-512 instance
            return CreateGenericHMACHandleFromPrimitiveProvider(Constants.BCRYPT_SHA512_ALGORITHM);
        }
    }
}
