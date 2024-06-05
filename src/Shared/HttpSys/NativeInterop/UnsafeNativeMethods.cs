// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static unsafe partial class UnsafeNclNativeMethods
{
    private const string sspicli_LIB = "sspicli.dll";
    private const string api_ms_win_core_io_LIB = "api-ms-win-core-io-l1-1-0.dll";
    private const string api_ms_win_core_handle_LIB = "api-ms-win-core-handle-l1-1-0.dll";
    private const string api_ms_win_core_heap_LIB = "api-ms-win-core-heap-L1-2-0.dll";
    private const string api_ms_win_core_heap_obsolete_LIB = "api-ms-win-core-heap-obsolete-L1-1-0.dll";
    private const string api_ms_win_core_kernel32_legacy_LIB = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";

    private const string TOKENBINDING = "tokenbinding.dll";

    // CONSIDER: Make this an enum, requires changing a lot of types from uint to ErrorCodes.
    internal static class ErrorCodes
    {
        internal const uint ERROR_SUCCESS = 0;
        internal const uint ERROR_FILE_NOT_FOUND = 2;
        internal const uint ERROR_ACCESS_DENIED = 5;
        internal const uint ERROR_SHARING_VIOLATION = 32;
        internal const uint ERROR_HANDLE_EOF = 38;
        internal const uint ERROR_NOT_SUPPORTED = 50;
        internal const uint ERROR_INVALID_PARAMETER = 87;
        internal const uint ERROR_INVALID_NAME = 123;
        internal const uint ERROR_ALREADY_EXISTS = 183;
        internal const uint ERROR_MORE_DATA = 234;
        internal const uint ERROR_OPERATION_ABORTED = 995;
        internal const uint ERROR_IO_PENDING = 997;
        internal const uint ERROR_NOT_FOUND = 1168;
        internal const uint ERROR_CONNECTION_INVALID = 1229;
    }

    [LibraryImport(api_ms_win_core_io_LIB, SetLastError = true)]
    internal static unsafe partial uint CancelIoEx(SafeHandle handle, SafeNativeOverlapped overlapped);

    [LibraryImport(api_ms_win_core_kernel32_legacy_LIB, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static unsafe partial bool SetFileCompletionNotificationModes(SafeHandle handle, FileCompletionNotificationModes modes);

    [Flags]
    internal enum FileCompletionNotificationModes : byte
    {
        None = 0,
        SkipCompletionPortOnSuccess = 1,
        SkipSetEventOnHandle = 2
    }

    [LibraryImport(TOKENBINDING)]
    public static partial int TokenBindingVerifyMessage(
        byte* tokenBindingMessage,
        uint tokenBindingMessageSize,
        char* keyType,
        byte* tlsUnique,
        uint tlsUniqueSize,
        out HeapAllocHandle resultList);

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa366569(v=vs.85).aspx
    [LibraryImport(api_ms_win_core_heap_LIB, SetLastError = true)]
    internal static partial IntPtr GetProcessHeap();

    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa366701(v=vs.85).aspx
    [LibraryImport(api_ms_win_core_heap_LIB, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HeapFree(
        IntPtr hHeap,
        uint dwFlags,
        IntPtr lpMem);

    internal static partial class SafeNetHandles
    {
        [LibraryImport(sspicli_LIB, SetLastError = true)]
        internal static partial int FreeContextBuffer(
            IntPtr contextBuffer);

        [LibraryImport(api_ms_win_core_handle_LIB, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr handle);

        [LibraryImport(api_ms_win_core_heap_obsolete_LIB, EntryPoint = "LocalAlloc", SetLastError = true)]
        internal static partial SafeLocalFreeChannelBinding LocalAllocChannelBinding(int uFlags, UIntPtr sizetdwBytes);

        [LibraryImport(api_ms_win_core_heap_obsolete_LIB, SetLastError = true)]
        internal static partial IntPtr LocalFree(IntPtr handle);
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
}
