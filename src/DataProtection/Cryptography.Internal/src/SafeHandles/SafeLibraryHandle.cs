// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

/// <summary>
/// Represents a handle to a Windows module (DLL).
/// </summary>
internal sealed unsafe partial class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    // Called by P/Invoke when returning SafeHandles
    public SafeLibraryHandle()
        : base(ownsHandle: true)
    { }

    /// <summary>
    /// Returns a value stating whether the library exports a given proc.
    /// </summary>
    public bool DoesProcExist(string lpProcName)
    {
        IntPtr pfnProc = UnsafeNativeMethods.GetProcAddress(this, lpProcName);
        return (pfnProc != IntPtr.Zero);
    }

    /// <summary>
    /// Forbids this library from being unloaded. The library will remain loaded until process termination,
    /// regardless of how many times FreeLibrary is called.
    /// </summary>
    public void ForbidUnload()
    {
        // from winbase.h
        const uint GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004U;
        const uint GET_MODULE_HANDLE_EX_FLAG_PIN = 0x00000001U;

        bool retVal = UnsafeNativeMethods.GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN, this, out _);
        if (!retVal)
        {
            UnsafeNativeMethods.ThrowExceptionForLastWin32Error();
        }
    }

    /// <summary>
    /// Formats a message string using the resource table in the specified library.
    /// </summary>
    public string? FormatMessage(int messageId)
    {
        // from winbase.h
        const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        const uint FORMAT_MESSAGE_FROM_HMODULE = 0x00000800;
        const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;

        LocalAllocHandle messageHandle;
        int numCharsOutput = UnsafeNativeMethods.FormatMessage(
            dwFlags: FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
            lpSource: this,
            dwMessageId: (uint)messageId,
            dwLanguageId: 0 /* ignore current culture */,
            lpBuffer: out messageHandle,
            nSize: 0 /* unused */,
            Arguments: IntPtr.Zero /* unused */);

        if (numCharsOutput != 0 && messageHandle != null && !messageHandle.IsInvalid)
        {
            // Successfully retrieved the message.
            using (messageHandle)
            {
                return new string((char*)messageHandle.DangerousGetHandle(), 0, numCharsOutput).Trim();
            }
        }
        else
        {
            // Message not found - that's fine.
            return null;
        }
    }

    /// <summary>
    /// Gets a delegate pointing to a given export from this library.
    /// </summary>
    public TDelegate? GetProcAddress<TDelegate>(string lpProcName, bool throwIfNotFound = true) where TDelegate : class
    {
        IntPtr pfnProc = UnsafeNativeMethods.GetProcAddress(this, lpProcName);
        if (pfnProc == IntPtr.Zero)
        {
            if (throwIfNotFound)
            {
                UnsafeNativeMethods.ThrowExceptionForLastWin32Error();
            }
            else
            {
                return null;
            }
        }

        return Marshal.GetDelegateForFunctionPointer<TDelegate>(pfnProc);
    }

    /// <summary>
    /// Opens a library. If 'filename' is not a fully-qualified path, the default search path is used.
    /// </summary>
    public static SafeLibraryHandle Open(string filename)
    {
        const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800U; // from libloaderapi.h

        SafeLibraryHandle handle = UnsafeNativeMethods.LoadLibraryEx(filename, IntPtr.Zero, LOAD_LIBRARY_SEARCH_SYSTEM32);
        if (handle == null || handle.IsInvalid)
        {
            UnsafeNativeMethods.ThrowExceptionForLastWin32Error();
        }
        return handle;
    }

    // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
    protected override bool ReleaseHandle()
    {
        return UnsafeNativeMethods.FreeLibrary(handle);
    }

    [SuppressUnmanagedCodeSecurity]
    private static partial class UnsafeNativeMethods
    {
        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms679351(v=vs.85).aspx
#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll", EntryPoint = "FormatMessageW", SetLastError = true)]
        public static partial int FormatMessage(
#else
        [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", SetLastError = true)]
        public static extern int FormatMessage(
#endif
           uint dwFlags,
           SafeLibraryHandle lpSource,
           uint dwMessageId,
           uint dwLanguageId,
           out LocalAllocHandle lpBuffer,
           uint nSize,
           IntPtr Arguments
        );

        // http://msdn.microsoft.com/en-us/library/ms683152(v=vs.85).aspx
        [return: MarshalAs(UnmanagedType.Bool)]
#if NETSTANDARD2_0
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll")]
        internal static partial bool FreeLibrary(
#else
        [DllImport("kernel32.dll")]
        internal static extern bool FreeLibrary(
#endif
            IntPtr hModule);

        // http://msdn.microsoft.com/en-us/library/ms683200(v=vs.85).aspx
        [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)]
        internal static partial bool GetModuleHandleEx(
#else
        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)]
        internal static extern bool GetModuleHandleEx(
#endif
            uint dwFlags,
            SafeLibraryHandle lpModuleName, // can point to a location within the module if GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS is set
            out IntPtr phModule);

        // http://msdn.microsoft.com/en-us/library/ms683212(v=vs.85).aspx
#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial IntPtr GetProcAddress(
#else
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetProcAddress(
#endif
            SafeLibraryHandle hModule,
            [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms684179(v=vs.85).aspx
#if NET7_0_OR_GREATER
        [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true)]
        internal static partial SafeLibraryHandle LoadLibraryEx(
#else
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true)]
        internal static extern SafeLibraryHandle LoadLibraryEx(
#endif
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            IntPtr hFile,
            uint dwFlags);

#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return.
        [DoesNotReturn]
        internal static void ThrowExceptionForLastWin32Error()
        {
            int hr = Marshal.GetHRForLastWin32Error();
            Marshal.ThrowExceptionForHR(hr);
        }
#pragma warning restore CS8763 // A method marked [DoesNotReturn] should not return.
    }
}
