// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

#if NET45
using System.Runtime.ConstrainedExecution;
#endif

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Represents a handle to a Windows module (DLL).
    /// </summary>
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        private SafeLibraryHandle()
            : base(ownsHandle: true) { }

        /// <summary>
        /// Gets a delegate pointing to a given export from this library.
        /// </summary>
        public TDelegate GetProcAddress<TDelegate>(string lpProcName, bool throwIfNotFound = true) where TDelegate : class
        {
            Debug.Assert(typeof(TDelegate).GetTypeInfo().IsSubclassOf(typeof(Delegate)), "TDelegate must be a delegate type!");

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

            return (TDelegate)(object)Marshal.GetDelegateForFunctionPointer(pfnProc, typeof(TDelegate));
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
        /// Opens a library. If 'filename' is not a fully-qualified path, the default search path is used.
        /// </summary>
        public static SafeLibraryHandle Open(string filename)
        {
            SafeLibraryHandle handle = UnsafeNativeMethods.LoadLibrary(filename);
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
            private const string KERNEL32_LIB = "kernel32.dll";

            // http://msdn.microsoft.com/en-us/library/ms683152(v=vs.85).aspx
            [return: MarshalAs(UnmanagedType.Bool)]
#if NET45
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
            [DllImport(KERNEL32_LIB, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal static extern bool FreeLibrary(IntPtr hModule);

            // http://msdn.microsoft.com/en-us/library/ms683200(v=vs.85).aspx
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport(KERNEL32_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            internal static extern bool GetModuleHandleEx(
                [In] uint dwFlags,
                [In] SafeLibraryHandle lpModuleName, // can point to a location within the module if GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS is set
                [Out] out IntPtr phModule);

            // http://msdn.microsoft.com/en-us/library/ms683212(v=vs.85).aspx
            [DllImport(KERNEL32_LIB, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr GetProcAddress(
                [In] SafeLibraryHandle hModule,
                [In, MarshalAs(UnmanagedType.LPStr)] string lpProcName);

            // http://msdn.microsoft.com/en-us/library/ms684175(v=vs.85).aspx
            [DllImport(KERNEL32_LIB, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern SafeLibraryHandle LoadLibrary(
                [In, MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

            internal static void ThrowExceptionForLastWin32Error()
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}
