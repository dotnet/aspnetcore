// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Represents a handle to a Windows module (DLL).
/// </summary>
internal sealed unsafe partial class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    // Called by P/Invoke when returning SafeHandles
    public SafeLibraryHandle()
        : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Returns a value stating whether the library exports a given proc.
    /// </summary>
    public bool DoesProcExist(string lpProcName)
    {
        IntPtr pfnProc = UnsafeNativeMethods.GetProcAddress(this, lpProcName);
        return (pfnProc != IntPtr.Zero);
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
        return handle!;
    }

    // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
    protected override bool ReleaseHandle()
    {
        return UnsafeNativeMethods.FreeLibrary(handle);
    }

    [SuppressUnmanagedCodeSecurity]
    private static partial class UnsafeNativeMethods
    {
        // http://msdn.microsoft.com/en-us/library/ms683152(v=vs.85).aspx
        [return: MarshalAs(UnmanagedType.Bool)]
        [LibraryImport("kernel32.dll")]
        internal static partial bool FreeLibrary(IntPtr hModule);

        // http://msdn.microsoft.com/en-us/library/ms683212(v=vs.85).aspx
        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial IntPtr GetProcAddress(
            SafeLibraryHandle hModule,
            [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms684179(v=vs.85).aspx
        [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryExW", SetLastError = true)]
        internal static partial SafeLibraryHandle LoadLibraryEx(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            IntPtr hFile,
            uint dwFlags);

        internal static void ThrowExceptionForLastWin32Error()
        {
            int hr = Marshal.GetHRForLastWin32Error();
            Marshal.ThrowExceptionForHR(hr);
        }
    }
}
