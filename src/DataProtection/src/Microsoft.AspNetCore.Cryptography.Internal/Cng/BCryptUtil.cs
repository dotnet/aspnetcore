// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Cryptography.Cng
{
    /// <summary>
    /// Wraps utility BCRYPT APIs that don't work directly with handles.
    /// </summary>
    internal unsafe static class BCryptUtil
    {
        /// <summary>
        /// Fills a buffer with cryptographically secure random data.
        /// </summary>
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
