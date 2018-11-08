// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Cryptography;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Wrappers around CryptProtectMemory / CryptUnprotectMemory.
    /// </summary>
    internal unsafe static class MemoryProtection
    {
        // from dpapi.h
        private const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;

        public static void CryptProtectMemory(SafeHandle pBuffer, uint byteCount)
        {
            if (!UnsafeNativeMethods.CryptProtectMemory(pBuffer, byteCount, CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                UnsafeNativeMethods.ThrowExceptionForLastCrypt32Error();
            }
        }

        public static void CryptUnprotectMemory(byte* pBuffer, uint byteCount)
        {
            if (!UnsafeNativeMethods.CryptUnprotectMemory(pBuffer, byteCount, CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                UnsafeNativeMethods.ThrowExceptionForLastCrypt32Error();
            }
        }

        public static void CryptUnprotectMemory(SafeHandle pBuffer, uint byteCount)
        {
            if (!UnsafeNativeMethods.CryptUnprotectMemory(pBuffer, byteCount, CRYPTPROTECTMEMORY_SAME_PROCESS))
            {
                UnsafeNativeMethods.ThrowExceptionForLastCrypt32Error();
            }
        }
    }
}
