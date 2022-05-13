// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Cryptography.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography;

[SuppressUnmanagedCodeSecurity]
internal static unsafe class UnsafeNativeMethods
{
    internal const string BCRYPT_LIB = "bcrypt.dll";
    private static SafeLibraryHandle? _lazyBCryptLibHandle;

    private const string CRYPT32_LIB = "crypt32.dll";
    private static SafeLibraryHandle? _lazyCrypt32LibHandle;

    private const string NCRYPT_LIB = "ncrypt.dll";
    private static SafeLibraryHandle? _lazyNCryptLibHandle;

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
#if NETSTANDARD2_0
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375399(v=vs.85).aspx
    internal static extern int BCryptDestroyHash(
        [In] IntPtr hHash);

    [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
#if NETSTANDARD2_0
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
        [In, MarshalAs(UnmanagedType.LPWStr)] string? pszImplementation,
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

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380262(v=vs.85).aspx
    [DllImport(CRYPT32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    public static extern bool CryptProtectMemory(
        [In] SafeHandle pData,
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
#if NETSTANDARD2_0
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
    // https://msdn.microsoft.com/en-us/library/windows/desktop/hh706801(v=vs.85).aspx
    internal static extern int NCryptGetProtectionDescriptorInfo(
        [In] NCryptDescriptorHandle hDescriptor,
        [In] IntPtr pMemPara,
        [In] uint dwInfoType,
        [Out] out LocalAllocHandle ppvInfo);

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

    [DllImport(NCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706811(v=vs.85).aspx
    internal static extern int NCryptUnprotectSecret(
       [Out] out NCryptDescriptorHandle phDescriptor,
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
    private static SafeLibraryHandle GetLibHandle(string libraryName, ref SafeLibraryHandle? safeLibraryHandle)
    {
        if (safeLibraryHandle is null)
        {
            var newHandle = SafeLibraryHandle.Open(libraryName);
            if (Interlocked.CompareExchange(ref safeLibraryHandle, newHandle, null) is not null)
            {
                newHandle.Dispose();
            }
        }

        return safeLibraryHandle;
    }

    // We use methods instead of properties to access lazy handles in order to prevent debuggers from automatically attempting to load libraries on unsupported platforms.
    private static SafeLibraryHandle GetBCryptLibHandle() => GetLibHandle(BCRYPT_LIB, ref _lazyBCryptLibHandle);
    private static SafeLibraryHandle GetCrypt32LibHandle() => GetLibHandle(CRYPT32_LIB, ref _lazyCrypt32LibHandle);
    private static SafeLibraryHandle GetNCryptLibHandle() => GetLibHandle(NCRYPT_LIB, ref _lazyNCryptLibHandle);

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
        var message = GetBCryptLibHandle().FormatMessage(ntstatus);
        throw new CryptographicException(message);
    }

    public static void ThrowExceptionForLastCrypt32Error()
    {
        var lastError = Marshal.GetLastWin32Error();
        Debug.Assert(lastError != 0, "This method should only be called if there was an error.");

        var message = GetCrypt32LibHandle().FormatMessage(lastError);
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
        var message = GetNCryptLibHandle().FormatMessage(ntstatus);
        throw new CryptographicException(message);
    }
}
