// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.SafeHandles;
using Microsoft.Win32.SafeHandles;

#if !ASPNETCORE50
using System.Runtime.ConstrainedExecution;
#endif

namespace Microsoft.AspNet.Security.DataProtection
{
#if !ASPNETCORE50
    [SuppressUnmanagedCodeSecurity]
#endif
    internal unsafe static class UnsafeNativeMethods
    {
        private const string BCRYPT_LIB = "bcrypt.dll";
        private static readonly SafeLibraryHandle _bcryptLibHandle = SafeLibraryHandle.Open(BCRYPT_LIB);

        private const string CRYPT32_LIB = "crypt32.dll";
        private static readonly SafeLibraryHandle _crypt32LibHandle = SafeLibraryHandle.Open(CRYPT32_LIB);

        private const string NCRYPT_LIB = "ncrypt.dll";
        private static readonly SafeLibraryHandle _ncryptLibHandle = SafeLibraryHandle.Open(NCRYPT_LIB);

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
            [In] void* pPaddingInfo,
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
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375399(v=vs.85).aspx
        internal static extern int BCryptDestroyHash(
            [In] IntPtr hHash);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
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
            [In] void* pPaddingInfo,
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
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375453(v=vs.85).aspx
        internal static extern int BCryptGenerateSymmetricKey(
            [In] BCryptAlgorithmHandle hAlgorithm,
            [Out] out BCryptKeyHandle phKey,
            [In] IntPtr pbKeyObject,
            [In] uint cbKeyObject,
            [In] byte* pbSecret,
            [In] uint cbSecret,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375458(v=vs.85).aspx
        internal static extern int BCryptGenRandom(
            [In] IntPtr hAlgorithm,
            [In] byte* pbBuffer,
            [In] uint cbBuffer,
            [In] BCryptGenRandomFlags dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375464(v=vs.85).aspx
        internal static extern int BCryptGetProperty(
            [In] BCryptHandle hObject,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            [In] void* pbOutput,
            [In] uint cbOutput,
            [Out] out uint pcbResult,
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375468(v=vs.85).aspx
        internal static extern int BCryptHashData(
            [In] BCryptHashHandle hHash,
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
            [In] uint dwFlags);

        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375504(v=vs.85).aspx
        internal static extern int BCryptSetProperty(
            [In] BCryptHandle hObject,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            [In] void* pbInput,
            [In] uint cbInput,
            [In] uint dwFlags);

        /*
         * CRYPT32.DLL
         */

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa376045(v=vs.85).aspx
        internal static extern SafeCertContextHandle CertDuplicateCertificateContext(
            [In] IntPtr pCertContext);

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa376075(v=vs.85).aspx
        internal static extern bool CertFreeCertificateContext(
            [In] IntPtr pCertContext);

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa376079(v=vs.85).aspx
        internal static extern bool CertGetCertificateContextProperty(
            [In] SafeCertContextHandle pCertContext,
            [In] uint dwPropId,
            [In] void* pvData,
            [In, Out] ref uint pcbData);

        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa379885(v=vs.85).aspx
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
#endif
        internal static extern bool CryptAcquireCertificatePrivateKey(
            [In] SafeCertContextHandle pCert,
            [In] uint dwFlags,
            [In] void* pvParameters,
            [Out] out SafeNCryptKeyHandle phCryptProvOrNCryptKey,
            [Out] out uint pdwKeySpec,
            [Out] out bool pfCallerFreeProvOrNCryptKey);

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
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380882(v=vs.85).aspx
        internal static extern bool CryptUnprotectData(
            [In] DATA_BLOB* pDataIn,
            [In] IntPtr ppszDataDescr,
            [In] DATA_BLOB* pOptionalEntropy,
            [In] IntPtr pvReserved,
            [In] IntPtr pPromptStruct,
            [In] uint dwFlags,
            [Out] out DATA_BLOB pDataOut);

        /*
       * CRYPT32.DLL
       */

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380262(v=vs.85).aspx
        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool CryptProtectMemory(
            [In] SafeHandle pData,
            [In] uint cbData,
            [In] uint dwFlags);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380890(v=vs.85).aspx
        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool CryptUnprotectMemory(
            [In] byte* pData,
            [In] uint cbData,
            [In] uint dwFlags);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380890(v=vs.85).aspx
        [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool CryptUnprotectMemory(
            [In] SafeHandle pData,
            [In] uint cbData,
            [In] uint dwFlags);

        /*
         * NCRYPT.DLL
         */

        [DllImport(NCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706799(v=vs.85).aspx
        internal static extern int NCryptCloseProtectionDescriptor(
            [In] IntPtr hDescriptor);

        [DllImport(NCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706800(v=vs.85).aspx
        internal static extern int NCryptCreateProtectionDescriptor(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwszDescriptorString,
            [In] uint dwFlags,
            [Out] out NCryptDescriptorHandle phDescriptor);

        [DllImport(NCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa376249(v=vs.85).aspx
        internal static extern int NCryptDecrypt(
            [In] SafeNCryptKeyHandle hKey,
            [In] byte* pbInput,
            [In] uint cbInput,
            [In] void* pPaddingInfo,
            [In] byte* pbOutput,
            [In] uint cbOutput,
            [Out] out uint pcbResult,
            [In] NCryptEncryptFlags dwFlags);

        [DllImport(NCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706802(v=vs.85).aspx
        internal static extern int NCryptProtectSecret(
            [In] NCryptDescriptorHandle hDescriptor,
            [In] uint dwFlags,
            [In] byte* pbData,
            [In] uint cbData,
            [In] IntPtr pMemPara,
            [In] IntPtr hWnd,
            [Out] out LocalAllocHandle ppbProtectedBlob,
            [Out] out uint pcbProtectedBlob);

        [DllImport(NCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706811(v=vs.85).aspx
        internal static extern int NCryptUnprotectSecret(
            [In] IntPtr phDescriptor,
            [In] uint dwFlags,
            [In] byte* pbProtectedBlob,
            [In] uint cbProtectedBlob,
            [In] IntPtr pMemPara,
            [In] IntPtr hWnd,
            [Out] out LocalAllocHandle ppbData,
            [Out] out uint pcbData);

        /*
         * HELPER FUNCTIONS
         */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowExceptionForBCryptStatus(int ntstatus)
        {
            // This wrapper method exists because 'throw' statements won't always be inlined.
            if (ntstatus != 0)
            {
                ThrowExceptionForBCryptStatusImpl(ntstatus);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExceptionForBCryptStatusImpl(int ntstatus)
        {
            string message = _bcryptLibHandle.FormatMessage(ntstatus);
            throw new CryptographicException(message);
        }

        public static void ThrowExceptionForLastCrypt32Error()
        {
            int lastError = Marshal.GetLastWin32Error();
            Debug.Assert(lastError != 0, "This method should only be called if there was an error.");

            string message = _crypt32LibHandle.FormatMessage(lastError);
            throw new CryptographicException(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowExceptionForNCryptStatus(int ntstatus)
        {
            // This wrapper method exists because 'throw' statements won't always be inlined.
            if (ntstatus != 0)
            {
                ThrowExceptionForNCryptStatusImpl(ntstatus);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowExceptionForNCryptStatusImpl(int ntstatus)
        {
            string message = _ncryptLibHandle.FormatMessage(ntstatus);
            throw new CryptographicException(message);
        }
    }
}
