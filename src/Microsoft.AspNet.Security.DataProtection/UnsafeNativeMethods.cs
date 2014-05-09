// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.AspNet.Security.DataProtection
{
    [SuppressUnmanagedCodeSecurity]
    internal unsafe static class UnsafeNativeMethods
    {
        private const string BCRYPT_LIB = "bcrypt.dll";
        private const string CRYPT32_LIB = "crypt32.dll";
        private const string KERNEL32_LIB = "kernel32.dll";

        /*
         * BCRYPT.DLL
         */

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375377(v=vs.85).aspx
        internal static extern int BCryptCloseAlgorithmProvider(
            [In] IntPtr hAlgorithm,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375383(v=vs.85).aspx
        internal static extern int BCryptCreateHash(
            [In] BCryptAlgorithmHandle hAlgorithm,
            [Out] out BCryptHashHandle phHash,
            [In] IntPtr pbHashObject,
            [In] uint cbHashObject,
            [In] byte* pbSecret,
            [In] uint cbSecret,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375391(v=vs.85).aspx
        internal static extern int BCryptDecrypt(
            [In] BCryptKeyHandle hKey,
            [In] byte* pbInput,
            [In] uint cbInput,
            [In] IntPtr pPaddingInfo,
            [In] byte* pbIV,
            [In] uint cbIV,
            [In] byte* pbOutput,
            [In] uint cbOutput,
            [Out] out uint pcbResult,
            [In] BCryptEncryptFlags dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/dd433795(v=vs.85).aspx
        internal static extern int BCryptDeriveKeyPBKDF2(
            [In] BCryptAlgorithmHandle hPrf,
            [In] byte* pbPassword,
            [In] uint cbPassword,
            [In] byte* pbSalt,
            [In] uint cbSalt,
            [In] ulong cIterations,
            [In] byte* pbDerivedKey,
            [In] uint cbDerivedKey,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375399(v=vs.85).aspx
        internal static extern int BCryptDestroyHash(
            [In] IntPtr hHash);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375404(v=vs.85).aspx
        internal static extern int BCryptDestroyKey(
            [In] IntPtr hKey);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375413(v=vs.85).aspx
        internal static extern int BCryptDuplicateHash(
            [In] BCryptHashHandle hHash,
            [Out] out BCryptHashHandle phNewHash,
            [In] IntPtr pbHashObject,
            [In] uint cbHashObject,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375421(v=vs.85).aspx
        internal static extern int BCryptEncrypt(
            [In] BCryptKeyHandle hKey,
            [In] byte* pbInput,
            [In] uint cbInput,
            [In] IntPtr pPaddingInfo,
            [In] byte* pbIV,
            [In] uint cbIV,
            [In] byte* pbOutput,
            [In] uint cbOutput,
            [Out] out uint pcbResult,
            [In] BCryptEncryptFlags dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375443(v=vs.85).aspx
        internal static extern int BCryptFinishHash(
            [In] BCryptHashHandle hHash,
            [In] byte* pbOutput,
            [In] uint cbOutput,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375458(v=vs.85).aspx
        internal static extern int BCryptGenRandom(
            [In] IntPtr hAlgorithm,
            [In] byte* pbBuffer,
            [In] uint cbBuffer,
            [In] BCryptGenRandomFlags dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375468(v=vs.85).aspx
        internal static extern int BCryptHashData(
            [In] BCryptHashHandle hHash,
            [In] byte* pbInput,
            [In] uint cbInput,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375475(v=vs.85).aspx
        internal static extern int BCryptImportKey(
            [In] BCryptAlgorithmHandle hAlgorithm,
            [In] IntPtr hImportKey, // unused
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszBlobType,
            [Out] out BCryptKeyHandle phKey,
            [In] IntPtr pbKeyObject, // unused
            [In] uint cbKeyObject,
            [In] byte* pbInput,
            [In] uint cbInput,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh448506(v=vs.85).aspx
        internal static extern int BCryptKeyDerivation(
            [In] BCryptKeyHandle hKey,
            [In] BCryptBufferDesc* pParameterList,
            [In] byte* pbDerivedKey,
            [In] uint cbDerivedKey,
            [Out] out uint pcbResult,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375479(v=vs.85).aspx
        internal static extern int BCryptOpenAlgorithmProvider(
            [Out] out BCryptAlgorithmHandle phAlgorithm,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszImplementation,
            [In] BCryptAlgorithmFlags dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375504(v=vs.85).aspx
        internal static extern int BCryptSetProperty(
            [In] SafeHandle hObject,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            [In] IntPtr pbInput,
            [In] uint cbInput,
            [In] uint dwFlags);

        /*
         * CRYPT32.DLL
         */

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380261(v=vs.85).aspx
        internal static extern bool CryptProtectData(
            [In] DATA_BLOB* pDataIn,
            [In] IntPtr szDataDescr,
            [In] DATA_BLOB* pOptionalEntropy,
            [In] IntPtr pvReserved,
            [In] IntPtr pPromptStruct,
            [In] uint dwFlags,
            [Out] out DATA_BLOB pDataOut);

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380262(v=vs.85).aspx
        internal static extern bool CryptProtectMemory(
            [In] byte* pData,
            [In] uint cbData,
            [In] uint dwFlags);

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380882(v=vs.85).aspx
        internal static extern bool CryptUnprotectData(
            [In] DATA_BLOB* pDataIn,
            [In] IntPtr ppszDataDescr,
            [In] DATA_BLOB* pOptionalEntropy,
            [In] IntPtr pvReserved,
            [In] IntPtr pPromptStruct,
            [In] uint dwFlags,
            [Out] out DATA_BLOB pDataOut);

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380890(v=vs.85).aspx
        internal static extern bool CryptUnprotectMemory(
            [In] byte* pData,
            [In] uint cbData,
            [In] uint dwFlags);

        /*
         * KERNEL32.DLL
         */

        [DllImport(KERNEL32_LIB, CallingConvention = CallingConvention.Winapi)]
        internal static extern void RtlZeroMemory(
            [In] IntPtr Destination,
            [In] UIntPtr /* SIZE_T */ Length);
    }
}
