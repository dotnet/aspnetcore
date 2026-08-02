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
internal static unsafe partial class UnsafeNativeMethods
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
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375377(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptCloseAlgorithmProvider(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptCloseAlgorithmProvider(
#endif
        IntPtr hAlgorithm,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375383(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptCreateHash(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptCreateHash(
#endif
        BCryptAlgorithmHandle hAlgorithm,
        out BCryptHashHandle phHash,
        IntPtr pbHashObject,
        uint cbHashObject,
        byte* pbSecret,
        uint cbSecret,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375391(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptDecrypt(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptDecrypt(
#endif
        BCryptKeyHandle hKey,
        byte* pbInput,
        uint cbInput,
        void* pPaddingInfo,
        byte* pbIV,
        uint cbIV,
        byte* pbOutput,
        uint cbOutput,
        out uint pcbResult,
        BCryptEncryptFlags dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/dd433795(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptDeriveKeyPBKDF2(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptDeriveKeyPBKDF2(
#endif
        BCryptAlgorithmHandle hPrf,
        byte* pbPassword,
        uint cbPassword,
        byte* pbSalt,
        uint cbSalt,
        ulong cIterations,
        byte* pbDerivedKey,
        uint cbDerivedKey,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375399(v=vs.85).aspx
#if NETSTANDARD2_0
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptDestroyHash(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptDestroyHash(
#endif
        IntPtr hHash);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375404(v=vs.85).aspx
#if NETSTANDARD2_0
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptDestroyKey(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptDestroyKey(
#endif
        IntPtr hKey);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375413(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptDuplicateHash(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptDuplicateHash(
#endif
        BCryptHashHandle hHash,
        out BCryptHashHandle phNewHash,
        IntPtr pbHashObject,
        uint cbHashObject,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375421(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptEncrypt(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptEncrypt(
#endif
        BCryptKeyHandle hKey,
        byte* pbInput,
        uint cbInput,
        void* pPaddingInfo,
        byte* pbIV,
        uint cbIV,
        byte* pbOutput,
        uint cbOutput,
        out uint pcbResult,
        BCryptEncryptFlags dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375443(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptFinishHash(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptFinishHash(
#endif
        BCryptHashHandle hHash,
        byte* pbOutput,
        uint cbOutput,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375453(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptGenerateSymmetricKey(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptGenerateSymmetricKey(
#endif
        BCryptAlgorithmHandle hAlgorithm,
        out BCryptKeyHandle phKey,
        IntPtr pbKeyObject,
        uint cbKeyObject,
        byte* pbSecret,
        uint cbSecret,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375458(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptGenRandom(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptGenRandom(
#endif
        IntPtr hAlgorithm,
        byte* pbBuffer,
        uint cbBuffer,
        BCryptGenRandomFlags dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375464(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptGetProperty(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptGetProperty(
#endif
        BCryptHandle hObject,
        [MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
        void* pbOutput,
        uint cbOutput,
        out uint pcbResult,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375468(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptHashData(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptHashData(
#endif
        BCryptHashHandle hHash,
        byte* pbInput,
        uint cbInput,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh448506(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptKeyDerivation(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptKeyDerivation(
#endif
        BCryptKeyHandle hKey,
        BCryptBufferDesc* pParameterList,
        byte* pbDerivedKey,
        uint cbDerivedKey,
        out uint pcbResult,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375479(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptOpenAlgorithmProvider(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptOpenAlgorithmProvider(
#endif
        out BCryptAlgorithmHandle phAlgorithm,
        [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
        [MarshalAs(UnmanagedType.LPWStr)] string? pszImplementation,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375504(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(BCRYPT_LIB)]
    internal static partial int BCryptSetProperty(
#else
    [DllImport(BCRYPT_LIB)]
    internal static extern int BCryptSetProperty(
#endif
        BCryptHandle hObject,
        [MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
        void* pbInput,
        uint cbInput,
        uint dwFlags);

    /*
     * CRYPT32.DLL
     */

    [return: MarshalAs(UnmanagedType.Bool)]
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380261(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(CRYPT32_LIB, SetLastError = true)]
    internal static partial bool CryptProtectData(
#else
    [DllImport(CRYPT32_LIB, SetLastError = true)]
    internal static extern bool CryptProtectData(
#endif
        DATA_BLOB* pDataIn,
        IntPtr szDataDescr,
        DATA_BLOB* pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        DATA_BLOB* pDataOut);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380262(v=vs.85).aspx
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(CRYPT32_LIB, SetLastError = true)]
    public static partial bool CryptProtectMemory(
#else
    [DllImport(CRYPT32_LIB, SetLastError = true)]
    public static extern bool CryptProtectMemory(
#endif
        SafeHandle pData,
        uint cbData,
        uint dwFlags);

    [return: MarshalAs(UnmanagedType.Bool)]
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380882(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(CRYPT32_LIB, SetLastError = true)]
    internal static partial bool CryptUnprotectData(
#else
    [DllImport(CRYPT32_LIB, SetLastError = true)]
    internal static extern bool CryptUnprotectData(
#endif
        DATA_BLOB* pDataIn,
        IntPtr ppszDataDescr,
        DATA_BLOB* pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        DATA_BLOB* pDataOut);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380890(v=vs.85).aspx
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(CRYPT32_LIB, SetLastError = true)]
    public static partial bool CryptUnprotectMemory(
#else
    [DllImport(CRYPT32_LIB, SetLastError = true)]
    public static extern bool CryptUnprotectMemory(
#endif
        byte* pData,
        uint cbData,
        uint dwFlags);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa380890(v=vs.85).aspx
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(CRYPT32_LIB, SetLastError = true)]
    public static partial bool CryptUnprotectMemory(
#else
    [DllImport(CRYPT32_LIB, SetLastError = true)]
    public static extern bool CryptUnprotectMemory(
#endif
        SafeHandle pData,
        uint cbData,
        uint dwFlags);

    /*
     * NCRYPT.DLL
     */

    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706799(v=vs.85).aspx
#if NETSTANDARD2_0
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
#if NET7_0_OR_GREATER
    [LibraryImport(NCRYPT_LIB)]
    internal static partial int NCryptCloseProtectionDescriptor(
#else
    [DllImport(NCRYPT_LIB)]
    internal static extern int NCryptCloseProtectionDescriptor(
#endif
        IntPtr hDescriptor);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706800(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(NCRYPT_LIB)]
    internal static partial int NCryptCreateProtectionDescriptor(
#else
    [DllImport(NCRYPT_LIB)]
    internal static extern int NCryptCreateProtectionDescriptor(
#endif
        [MarshalAs(UnmanagedType.LPWStr)] string pwszDescriptorString,
        uint dwFlags,
        out NCryptDescriptorHandle phDescriptor);

    // https://msdn.microsoft.com/en-us/library/windows/desktop/hh706801(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(NCRYPT_LIB)]
    internal static partial int NCryptGetProtectionDescriptorInfo(
#else
    [DllImport(NCRYPT_LIB)]
    internal static extern int NCryptGetProtectionDescriptorInfo(
#endif
        NCryptDescriptorHandle hDescriptor,
        IntPtr pMemPara,
        uint dwInfoType,
        out LocalAllocHandle ppvInfo);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706802(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(NCRYPT_LIB)]
    internal static partial int NCryptProtectSecret(
#else
    [DllImport(NCRYPT_LIB)]
    internal static extern int NCryptProtectSecret(
#endif
        NCryptDescriptorHandle hDescriptor,
        uint dwFlags,
        byte* pbData,
        uint cbData,
        IntPtr pMemPara,
        IntPtr hWnd,
        out LocalAllocHandle ppbProtectedBlob,
        out uint pcbProtectedBlob);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706811(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(NCRYPT_LIB)]
    internal static partial int NCryptUnprotectSecret(
#else
    [DllImport(NCRYPT_LIB)]
    internal static extern int NCryptUnprotectSecret(
#endif
        IntPtr phDescriptor,
        uint dwFlags,
        byte* pbProtectedBlob,
        uint cbProtectedBlob,
        IntPtr pMemPara,
        IntPtr hWnd,
        out LocalAllocHandle ppbData,
        out uint pcbData);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/hh706811(v=vs.85).aspx
#if NET7_0_OR_GREATER
    [LibraryImport(NCRYPT_LIB)]
    internal static partial int NCryptUnprotectSecret(
#else
    [DllImport(NCRYPT_LIB)]
    internal static extern int NCryptUnprotectSecret(
#endif
       out NCryptDescriptorHandle phDescriptor,
       uint dwFlags,
       byte* pbProtectedBlob,
       uint cbProtectedBlob,
       IntPtr pMemPara,
       IntPtr hWnd,
       out LocalAllocHandle ppbData,
       out uint pcbData);

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
