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
