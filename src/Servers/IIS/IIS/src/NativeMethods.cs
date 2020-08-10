// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.Server.IIS.Core;

namespace Microsoft.AspNetCore.Server.IIS
{
    internal static class NativeMethods
    {
        internal const int HR_OK = 0;
        internal const int ERROR_NOT_FOUND = unchecked((int)0x80070490);
        internal const int ERROR_OPERATION_ABORTED = unchecked((int)0x800703E3);
        internal const int ERROR_INVALID_PARAMETER = unchecked((int)0x80070057);
        internal const int ERROR_HANDLE_EOF = unchecked((int)0x80070026);

        private const string KERNEL32 = "kernel32.dll";

        internal const string AspNetCoreModuleDll = "aspnetcorev2_inprocess.dll";

        [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]

        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool IsAspNetCoreModuleLoaded()
        {
            return GetModuleHandle(AspNetCoreModuleDll) != IntPtr.Zero;
        }

        public enum REQUEST_NOTIFICATION_STATUS
        {
            RQ_NOTIFICATION_CONTINUE,
            RQ_NOTIFICATION_PENDING,
            RQ_NOTIFICATION_FINISH_REQUEST
        }

        public delegate REQUEST_NOTIFICATION_STATUS PFN_REQUEST_HANDLER(IntPtr pInProcessHandler, IntPtr pvRequestContext);
        public delegate void PFN_DISCONNECT_HANDLER(IntPtr pvManagedHttpContext);
        public delegate bool PFN_SHUTDOWN_HANDLER(IntPtr pvRequestContext);
        public delegate REQUEST_NOTIFICATION_STATUS PFN_ASYNC_COMPLETION(IntPtr pvManagedHttpContext, int hr, int bytes);
        public delegate REQUEST_NOTIFICATION_STATUS PFN_WEBSOCKET_ASYNC_COMPLETION(IntPtr pInProcessHandler, IntPtr completionInfo, IntPtr pvCompletionContext);
        public delegate void PFN_REQUESTS_DRAINED_HANDLER(IntPtr pvRequestContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_post_completion(HandlerSafeHandle pInProcessHandler, int cbBytes);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_completion_status(HandlerSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_indicate_completion(HandlerSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS notificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int register_callbacks(IntPtr pInProcessApplication,
            PFN_REQUEST_HANDLER requestCallback,
            PFN_SHUTDOWN_HANDLER shutdownCallback,
            PFN_DISCONNECT_HANDLER disconnectCallback,
            PFN_ASYNC_COMPLETION asyncCallback,
            PFN_REQUESTS_DRAINED_HANDLER requestsDrainedHandler,
            IntPtr pvRequestContext,
            IntPtr pvShutdownContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_write_response_bytes(HandlerSafeHandle pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_flush_response_bytes(HandlerSafeHandle pInProcessHandler, bool fMoreData, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe HttpApiTypes.HTTP_REQUEST_V2* http_get_raw_request(HandlerSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_stop_calls_into_managed(IntPtr pInProcessApplication);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_stop_incoming_requests(IntPtr pInProcessApplication);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_disable_buffering(HandlerSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll, CharSet = CharSet.Ansi)]
        private static extern int http_set_response_status_code(HandlerSafeHandle pInProcessHandler, ushort statusCode, string pszReason);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_read_request_bytes(HandlerSafeHandle pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_managed_context(HandlerSafeHandle pInProcessHandler, IntPtr pvManagedContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_application_properties(ref IISConfigurationData iiConfigData);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_server_variable(
            HandlerSafeHandle pInProcessHandler,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            [MarshalAs(UnmanagedType.BStr)] out string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_server_variable(
            HandlerSafeHandle pInProcessHandler,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_read_bytes(
            HandlerSafeHandle pInProcessHandler,
            byte* pvBuffer,
            int cbBuffer,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out int dwBytesReceived,
            out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_write_bytes(
            HandlerSafeHandle pInProcessHandler,
            HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks,
            int nChunks,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_enable_websockets(HandlerSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_cancel_io(HandlerSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_close_connection(HandlerSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_unknown_header(HandlerSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_has_response4(HandlerSafeHandle pInProcessHandler, out bool isResponse4);
        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_trailer(HandlerSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool replace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_reset_stream(HandlerSafeHandle pInProcessHandler, ulong errorCode);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_known_header(HandlerSafeHandle pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_authentication_information(HandlerSafeHandle pInProcessHandler, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_set_startup_error_page_content(byte* content, int contentLength);

        public static void HttpPostCompletion(HandlerSafeHandle pInProcessHandler, int cbBytes)
        {
            Validate(http_post_completion(pInProcessHandler, cbBytes));
        }

        public static void HttpSetCompletionStatus(HandlerSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus)
        {
            Validate(http_set_completion_status(pInProcessHandler, rquestNotificationStatus));
        }

        public static void HttpRegisterCallbacks(IntPtr pInProcessApplication,
            PFN_REQUEST_HANDLER requestCallback,
            PFN_SHUTDOWN_HANDLER shutdownCallback,
            PFN_DISCONNECT_HANDLER disconnectCallback,
            PFN_ASYNC_COMPLETION asyncCallback,
            PFN_REQUESTS_DRAINED_HANDLER requestsDrainedHandler,
            IntPtr pvRequestContext,
            IntPtr pvShutdownContext)
        {
            Validate(register_callbacks(pInProcessApplication, requestCallback, shutdownCallback, disconnectCallback, asyncCallback, requestsDrainedHandler, pvRequestContext, pvShutdownContext));
        }

        internal static unsafe int HttpWriteResponseBytes(HandlerSafeHandle pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
        {
            return http_write_response_bytes(pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);
        }

        public static int HttpFlushResponseBytes(HandlerSafeHandle pInProcessHandler, bool fMoreData, out bool fCompletionExpected)
        {
            return http_flush_response_bytes(pInProcessHandler, fMoreData, out fCompletionExpected);
        }

        internal static unsafe HttpApiTypes.HTTP_REQUEST_V2* HttpGetRawRequest(HandlerSafeHandle pInProcessHandler)
        {
            return http_get_raw_request(pInProcessHandler);
        }

        public static void HttpStopCallsIntoManaged(IntPtr pInProcessApplication)
        {
            Validate(http_stop_calls_into_managed(pInProcessApplication));
        }

        public static void HttpStopIncomingRequests(IntPtr pInProcessApplication)
        {
            Validate(http_stop_incoming_requests(pInProcessApplication));
        }

        public static void HttpDisableBuffering(HandlerSafeHandle pInProcessHandler)
        {
            Validate(http_disable_buffering(pInProcessHandler));
        }

        public static void HttpSetResponseStatusCode(HandlerSafeHandle pInProcessHandler, ushort statusCode, string pszReason)
        {
            Validate(http_set_response_status_code(pInProcessHandler, statusCode, pszReason));
        }

        public static unsafe int HttpReadRequestBytes(HandlerSafeHandle pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return http_read_request_bytes(pInProcessHandler, pvBuffer, cbBuffer, out dwBytesReceived, out fCompletionExpected);
        }

        public static void HttpGetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr)
        {
            http_get_completion_info(pCompletionInfo, out cbBytes, out hr);
        }

        public static void HttpSetManagedContext(HandlerSafeHandle pInProcessHandler, IntPtr pvManagedContext)
        {
            Validate(http_set_managed_context(pInProcessHandler, pvManagedContext));
        }

        internal static IISConfigurationData HttpGetApplicationProperties()
        {
            var iisConfigurationData = new IISConfigurationData();
            Validate(http_get_application_properties(ref iisConfigurationData));
            return iisConfigurationData;
        }

        public static bool HttpTryGetServerVariable(HandlerSafeHandle pInProcessHandler, string variableName, out string value)
        {
            return http_get_server_variable(pInProcessHandler, variableName, out value) == 0;
        }

        public static void HttpSetServerVariable(HandlerSafeHandle pInProcessHandler, string variableName, string value)
        {
            Validate(http_set_server_variable(pInProcessHandler, variableName, value));
        }

        public static unsafe int HttpWebsocketsReadBytes(
            HandlerSafeHandle pInProcessHandler,
            byte* pvBuffer,
            int cbBuffer,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext, out int dwBytesReceived,
            out bool fCompletionExpected)
        {
            return http_websockets_read_bytes(pInProcessHandler, pvBuffer, cbBuffer, pfnCompletionCallback, pvCompletionContext, out dwBytesReceived, out fCompletionExpected);
        }

        internal static unsafe int HttpWebsocketsWriteBytes(
            HandlerSafeHandle pInProcessHandler,
            HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks,
            int nChunks,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out bool fCompletionExpected)
        {
            return http_websockets_write_bytes(pInProcessHandler, pDataChunks, nChunks, pfnCompletionCallback, pvCompletionContext, out fCompletionExpected);
        }

        public static void HttpEnableWebsockets(HandlerSafeHandle pInProcessHandler)
        {
            Validate(http_enable_websockets(pInProcessHandler));
        }

        public static bool HttpTryCancelIO(HandlerSafeHandle pInProcessHandler)
        {
            var hr = http_cancel_io(pInProcessHandler);
            // ERROR_NOT_FOUND is expected if async operation finished
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363792(v=vs.85).aspx
            // ERROR_INVALID_PARAMETER is expected for "fake" requests like applicationInitialization ones
            if (hr == ERROR_NOT_FOUND || hr ==  ERROR_INVALID_PARAMETER)
            {
                return false;
            }
            Validate(hr);
            return true;
        }

        public static void HttpCloseConnection(HandlerSafeHandle pInProcessHandler)
        {
            Validate(http_close_connection(pInProcessHandler));
        }

        public static unsafe void HttpResponseSetUnknownHeader(HandlerSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace)
        {
            Validate(http_response_set_unknown_header(pInProcessHandler, pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace));
        }

        public static unsafe void HttpResponseSetKnownHeader(HandlerSafeHandle pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace)
        {
            Validate(http_response_set_known_header(pInProcessHandler, headerId, pHeaderValue, length, fReplace));
        }

        public static void HttpGetAuthenticationInformation(HandlerSafeHandle pInProcessHandler, out string authType, out IntPtr token)
        {
            Validate(http_get_authentication_information(pInProcessHandler, out authType, out token));
        }

        internal static unsafe void HttpSetStartupErrorPageContent(byte[] content)
        {
            fixed(byte* bytePtr = content)
            {
                http_set_startup_error_page_content(bytePtr, content.Length);
            }
        }

        internal static unsafe void HttpResponseSetTrailer(HandlerSafeHandle pInProcessHandler, byte* pHeaderName, byte* pHeaderValue, ushort length, bool replace)
        {
            Validate(http_response_set_trailer(pInProcessHandler, pHeaderName, pHeaderValue, length, false));
        }

        internal static unsafe void HttpResetStream(HandlerSafeHandle pInProcessHandler, ulong errorCode)
        {
            Validate(http_reset_stream(pInProcessHandler, errorCode));
        }

        internal static unsafe bool HttpSupportTrailer(HandlerSafeHandle pInProcessHandler)
        {
            bool supportsTrailers;
            Validate(http_has_response4(pInProcessHandler, out supportsTrailers));
            return supportsTrailers;
        }

        private static void Validate(int hr)
        {
            if (hr != HR_OK)
            {
                throw Marshal.GetExceptionForHR(hr);
            }
        }
    }
}
