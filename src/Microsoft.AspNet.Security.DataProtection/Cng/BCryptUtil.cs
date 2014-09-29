// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.Cng
{
    internal unsafe static class BCryptUtil
    {
        // helper function that's similar to RNGCryptoServiceProvider, but works directly with pointers
        public static void GenRandom(byte* pbBuffer, uint cbBuffer)
        {
            if (cbBuffer != 0)
            {
                int ntstatus = UnsafeNativeMethods.BCryptGenRandom(
                    hAlgorithm: IntPtr.Zero,
                    pbBuffer: pbBuffer,
                    cbBuffer: cbBuffer,
                    dwFlags: BCryptGenRandomFlags.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
                UnsafeNativeMethods.ThrowExceptionForBCryptStatus(ntstatus);
            }
        }
    }
}
