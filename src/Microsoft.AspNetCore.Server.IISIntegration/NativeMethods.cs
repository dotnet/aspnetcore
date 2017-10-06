// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class NativeMethods
    {
#if DOTNET5_4
        private const string api_ms_win_core_handle_LIB = "api-ms-win-core-handle-l1-1-0.dll";
#else
        private const string KERNEL32 = "kernel32.dll";
#endif

#if DOTNET5_4
        [DllImport(api_ms_win_core_handle_LIB, ExactSpelling = true, SetLastError = true)]
#else
        [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
        internal static extern bool CloseHandle(IntPtr handle);

        public const int S_OK = 0;
        private const string AspNetCoreModuleDll = "aspnetcore.dll";

        public enum REQUEST_NOTIFICATION_STATUS
        {
            RQ_NOTIFICATION_CONTINUE,
            RQ_NOTIFICATION_PENDING,
            RQ_NOTIFICATION_FINISH_REQUEST
        }

        public delegate REQUEST_NOTIFICATION_STATUS PFN_REQUEST_HANDLER(IntPtr pHttpContext, IntPtr pvRequestContext);
        public delegate bool PFN_SHUTDOWN_HANDLER(IntPtr pvRequestContext);
        public delegate REQUEST_NOTIFICATION_STATUS PFN_ASYNC_COMPLETION(IntPtr pHttpContext, IntPtr completionInfo, IntPtr pvCompletionContext);

        // TODO make this all internal
        [DllImport(AspNetCoreModuleDll)]
        public static extern int http_post_completion(IntPtr pHttpContext, int cbBytes);

        [DllImport(AspNetCoreModuleDll)]
        public static extern void http_indicate_completion(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS notificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        public static extern void register_callbacks(PFN_REQUEST_HANDLER request_callback, PFN_SHUTDOWN_HANDLER shutdown_callback, IntPtr pvRequestContext, IntPtr pvShutdownContext);

        [DllImport(AspNetCoreModuleDll)]
        internal unsafe static extern int http_write_response_bytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, PFN_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        public unsafe static extern int http_flush_response_bytes(IntPtr pHttpContext, PFN_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        internal unsafe static extern HttpApiTypes.HTTP_REQUEST_V2* http_get_raw_request(IntPtr pHttpContext);

        [DllImport(AspNetCoreModuleDll)]
        internal unsafe static extern HttpApiTypes.HTTP_RESPONSE_V2* http_get_raw_response(IntPtr pHttpContext);

        [DllImport(AspNetCoreModuleDll)]
        public unsafe static extern void http_set_response_status_code(IntPtr pHttpContext, ushort statusCode, byte* pszReason);

        [DllImport(AspNetCoreModuleDll)]
        public unsafe static extern int http_read_request_bytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, PFN_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        public unsafe static extern bool http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

        [DllImport(AspNetCoreModuleDll)]
        [return: MarshalAs(UnmanagedType.BStr)]
        public unsafe static extern string http_get_application_full_path();

        [DllImport(AspNetCoreModuleDll)]
        public unsafe static extern bool http_shutdown();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool is_ancm_loaded()
        {
            return GetModuleHandle(AspNetCoreModuleDll) != IntPtr.Zero;
        }
    }
}
