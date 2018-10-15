// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles
{
    /// <summary>
    /// Represents a handle to a Windows module (DLL).
    /// </summary>
    internal unsafe sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        private SafeLibraryHandle()
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

            IntPtr unused;
            bool retVal = UnsafeNativeMethods.GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN, this, out unused);
            if (!retVal)
            {
                UnsafeNativeMethods.ThrowExceptionForLastWin32Error();
            }
        }

        /// <summary>
        /// Formats a message string using the resource table in the specified library.
        /// </summary>
        public string FormatMessage(int messageId)
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
                    return new String((char*)messageHandle.DangerousGetHandle(), 0, numCharsOutput).Trim();
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
            // http://msdn.microsoft.com/en-us/library/windows/desktop/ms679351(v=vs.85).aspx
            [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int FormatMessage(
                [In] uint dwFlags,
                [In] SafeLibraryHandle lpSource,
                [In] uint dwMessageId,
                [In] uint dwLanguageId,
                [Out] out LocalAllocHandle lpBuffer,
                [In] uint nSize,
                [In] IntPtr Arguments
            );

            // http://msdn.microsoft.com/en-us/library/ms683152(v=vs.85).aspx
            [return: MarshalAs(UnmanagedType.Bool)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal static extern bool FreeLibrary(IntPtr hModule);

            // http://msdn.microsoft.com/en-us/library/ms683200(v=vs.85).aspx
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern bool GetModuleHandleEx(
                [In] uint dwFlags,
                [In] SafeLibraryHandle lpModuleName, // can point to a location within the module if GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS is set
                [Out] out IntPtr phModule);

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
