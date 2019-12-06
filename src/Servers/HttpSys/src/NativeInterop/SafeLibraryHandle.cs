// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// Represents a handle to a Windows module (DLL).
    /// </summary>
    internal unsafe sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        private SafeLibraryHandle()
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
        public TDelegate GetProcAddress<TDelegate>(string lpProcName, bool throwIfNotFound = true) where TDelegate : class
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
        private static class UnsafeNativeMethods
        {
            // http://msdn.microsoft.com/en-us/library/ms683152(v=vs.85).aspx
            [return: MarshalAs(UnmanagedType.Bool)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal static extern bool FreeLibrary(IntPtr hModule);

            // http://msdn.microsoft.com/en-us/library/ms683212(v=vs.85).aspx
            [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern IntPtr GetProcAddress(
                [In] SafeLibraryHandle hModule,
                [In, MarshalAs(UnmanagedType.LPStr)] string lpProcName);

            // http://msdn.microsoft.com/en-us/library/windows/desktop/ms684179(v=vs.85).aspx
            [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern SafeLibraryHandle LoadLibraryEx(
                [In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
                [In] IntPtr hFile,
                [In] uint dwFlags);

            internal static void ThrowExceptionForLastWin32Error()
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
