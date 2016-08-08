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

//------------------------------------------------------------------------------
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Http.Server
{
    internal static unsafe class UnsafeNclNativeMethods
    {
#if NETSTANDARD1_3
        private const string sspicli_LIB = "sspicli.dll";
        private const string api_ms_win_core_processthreads_LIB = "api-ms-win-core-processthreads-l1-1-1.dll";
        private const string api_ms_win_core_io_LIB = "api-ms-win-core-io-l1-1-1.dll";
        private const string api_ms_win_core_handle_LIB = "api-ms-win-core-handle-l1-1-0.dll";
        private const string api_ms_win_core_libraryloader_LIB = "api-ms-win-core-libraryloader-l1-1-0.dll";
        private const string api_ms_win_core_heap_LIB = "api-ms-win-core-heap-L1-2-0.dll";
        private const string api_ms_win_core_heap_obsolete_LIB = "api-ms-win-core-heap-obsolete-L1-1-0.dll";
        private const string api_ms_win_core_kernel32_legacy_LIB = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";
#else
        private const string KERNEL32 = "kernel32.dll";
        private const string SECUR32 = "secur32.dll";
#endif
        private const string TOKENBINDING = "tokenbinding.dll";

        // CONSIDER: Make this an enum, requires changing a lot of types from uint to ErrorCodes.
        internal static class ErrorCodes
        {
            internal const uint ERROR_SUCCESS = 0;
            internal const uint ERROR_HANDLE_EOF = 38;
            internal const uint ERROR_NOT_SUPPORTED = 50;
            internal const uint ERROR_INVALID_PARAMETER = 87;
            internal const uint ERROR_ALREADY_EXISTS = 183;
            internal const uint ERROR_MORE_DATA = 234;
            internal const uint ERROR_OPERATION_ABORTED = 995;
            internal const uint ERROR_IO_PENDING = 997;
            internal const uint ERROR_NOT_FOUND = 1168;
            internal const uint ERROR_CONNECTION_INVALID = 1229;
        }

#if NETSTANDARD1_3
        [DllImport(api_ms_win_core_io_LIB, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
#else
        [DllImport(KERNEL32, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
#endif
        internal static unsafe extern uint CancelIoEx(SafeHandle handle, SafeNativeOverlapped overlapped);

#if NETSTANDARD1_3
        [DllImport(api_ms_win_core_kernel32_legacy_LIB, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
#else
        [DllImport(KERNEL32, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
#endif
        internal static unsafe extern bool SetFileCompletionNotificationModes(SafeHandle handle, FileCompletionNotificationModes modes);

        [Flags]
        internal enum FileCompletionNotificationModes : byte
        {
            None = 0,
            SkipCompletionPortOnSuccess = 1,
            SkipSetEventOnHandle = 2
        }

        [DllImport(TOKENBINDING, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern int TokenBindingVerifyMessage(
            [In] byte* tokenBindingMessage,
            [In] uint tokenBindingMessageSize,
            [In] char* keyType,
            [In] byte* tlsUnique,
            [In] uint tlsUniqueSize,
            [Out] out HeapAllocHandle resultList);

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa366569(v=vs.85).aspx
#if NETSTANDARD1_3
        [DllImport(api_ms_win_core_heap_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
#else
        [DllImport(KERNEL32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
#endif
        internal static extern IntPtr GetProcessHeap();

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa366701(v=vs.85).aspx
#if NETSTANDARD1_3
        [DllImport(api_ms_win_core_heap_LIB, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
#else
        [DllImport(KERNEL32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
#endif
        internal static extern bool HeapFree(
            [In] IntPtr hHeap,
            [In] uint dwFlags,
            [In] IntPtr lpMem);

        internal static class SafeNetHandles
        {
#if NETSTANDARD1_3
            [DllImport(sspicli_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
#endif        
            internal static extern int FreeContextBuffer(
                [In] IntPtr contextBuffer);

#if NETSTANDARD1_3
            [DllImport(sspicli_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
#endif        
            internal static unsafe extern int QueryContextAttributesW(
                ref SSPIHandle contextHandle,
                [In] ContextAttribute attribute,
                [In] void* buffer);

#if NETSTANDARD1_3
            [DllImport(api_ms_win_core_handle_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            internal static extern bool CloseHandle(IntPtr handle);

#if NETSTANDARD1_3
            [DllImport(api_ms_win_core_heap_obsolete_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            internal static extern SafeLocalFree LocalAlloc(int uFlags, UIntPtr sizetdwBytes);

#if NETSTANDARD1_3
            [DllImport(api_ms_win_core_heap_obsolete_LIB, EntryPoint = "LocalAlloc", SetLastError = true)]
#else
            [DllImport(KERNEL32, EntryPoint = "LocalAlloc", SetLastError = true)]
#endif

            internal static extern SafeLocalFreeChannelBinding LocalAllocChannelBinding(int uFlags, UIntPtr sizetdwBytes);

#if NETSTANDARD1_3
            [DllImport(api_ms_win_core_heap_obsolete_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            internal static extern IntPtr LocalFree(IntPtr handle);

#if NETSTANDARD1_3
            [DllImport(api_ms_win_core_libraryloader_LIB, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
#endif            
            internal static extern unsafe SafeLoadLibrary LoadLibraryExW([In] string lpwLibFileName, [In] void* hFile, [In] uint dwFlags);

#if NETSTANDARD1_3
            [DllImport(api_ms_win_core_libraryloader_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            internal static extern unsafe bool FreeLibrary([In] IntPtr hModule);
        }

        // from tokenbinding.h
        internal static class TokenBinding
        {
            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct TOKENBINDING_RESULT_DATA
            {
                public uint identifierSize;
                public TOKENBINDING_IDENTIFIER* identifierData;
                public TOKENBINDING_EXTENSION_FORMAT extensionFormat;
                public uint extensionSize;
                public IntPtr extensionData;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct TOKENBINDING_IDENTIFIER
            {
                // Note: If the layout of these fields changes, be sure to make the
                // corresponding change to TokenBindingUtil.ExtractIdentifierBlob.

                public TOKENBINDING_TYPE bindingType;
                public TOKENBINDING_HASH_ALGORITHM hashAlgorithm;
                public TOKENBINDING_SIGNATURE_ALGORITHM signatureAlgorithm;
            }

            internal enum TOKENBINDING_TYPE : byte
            {
                TOKENBINDING_TYPE_PROVIDED = 0,
                TOKENBINDING_TYPE_REFERRED = 1,
            }

            internal enum TOKENBINDING_HASH_ALGORITHM : byte
            {
                TOKENBINDING_HASH_ALGORITHM_SHA256 = 4,
            }

            internal enum TOKENBINDING_SIGNATURE_ALGORITHM : byte
            {
                TOKENBINDING_SIGNATURE_ALGORITHM_RSA = 1,
                TOKENBINDING_SIGNATURE_ALGORITHM_ECDSAP256 = 3,
            }

            internal enum TOKENBINDING_EXTENSION_FORMAT
            {
                TOKENBINDING_EXTENSION_FORMAT_UNDEFINED = 0,
            }

            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct TOKENBINDING_RESULT_LIST
            {
                public uint resultCount;
                public TOKENBINDING_RESULT_DATA* resultData;
            }
        }

        // DACL related stuff

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated natively")]
        [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", 
            Justification = "Does not own the resource.")]
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public int nLength = 12;
            public SafeLocalMemHandle lpSecurityDescriptor = new SafeLocalMemHandle(IntPtr.Zero, false);
            public bool bInheritHandle = false;
        }
    }
}
