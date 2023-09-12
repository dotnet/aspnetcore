// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static unsafe partial class UnsafeNclNativeMethods
{
    private const string api_ms_win_core_io_LIB = "api-ms-win-core-io-l1-1-0.dll";
    private const string api_ms_win_core_handle_LIB = "api-ms-win-core-handle-l1-1-0.dll";
    private const string api_ms_win_core_heap_LIB = "api-ms-win-core-heap-L1-2-0.dll";
    private const string api_ms_win_core_heap_obsolete_LIB = "api-ms-win-core-heap-obsolete-L1-1-0.dll";
    private const string api_ms_win_core_kernel32_legacy_LIB = "api-ms-win-core-kernel32-legacy-l1-1-0.dll";

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
        [LibraryImport(api_ms_win_core_handle_LIB, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr handle);

        [LibraryImport(api_ms_win_core_heap_obsolete_LIB, SetLastError = true)]
        internal static partial IntPtr LocalFree(IntPtr handle);
    }
}
