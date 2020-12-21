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

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_post_completion(NativeSafeHandle pInProcessHandler, int cbBytes);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_completion_status(NativeSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_indicate_completion(NativeSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS notificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private unsafe static extern int register_callbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> requestCallback,
            delegate* unmanaged<IntPtr, int> shutdownCallback,
            delegate* unmanaged<IntPtr, void> disconnectCallback,
            delegate* unmanaged<IntPtr, int, int, REQUEST_NOTIFICATION_STATUS> asyncCallback,
            delegate* unmanaged<IntPtr, void> requestsDrainedHandler,
            IntPtr pvRequestContext,
            IntPtr pvShutdownContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_write_response_bytes(NativeSafeHandle pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_flush_response_bytes(NativeSafeHandle pInProcessHandler, bool fMoreData, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe HttpApiTypes.HTTP_REQUEST_V2* http_get_raw_request(NativeSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_stop_calls_into_managed(NativeSafeHandle pInProcessApplication);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_stop_incoming_requests(NativeSafeHandle pInProcessApplication);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_disable_buffering(NativeSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll, CharSet = CharSet.Ansi)]
        private static extern int http_set_response_status_code(NativeSafeHandle pInProcessHandler, ushort statusCode, string pszReason);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_read_request_bytes(NativeSafeHandle pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_managed_context(NativeSafeHandle pInProcessHandler, IntPtr pvManagedContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_application_properties(ref IISConfigurationData iiConfigData);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_server_variable(
            NativeSafeHandle pInProcessHandler,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            [MarshalAs(UnmanagedType.BStr)] out string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_server_variable(
            NativeSafeHandle pInProcessHandler,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_read_bytes(
            NativeSafeHandle pInProcessHandler,
            byte* pvBuffer,
            int cbBuffer,
            delegate* unmanaged<IntPtr, IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out int dwBytesReceived,
            out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_write_bytes(
            NativeSafeHandle pInProcessHandler,
            HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks,
            int nChunks,
            delegate* unmanaged<IntPtr, IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_enable_websockets(NativeSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_cancel_io(NativeSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_close_connection(NativeSafeHandle pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_unknown_header(NativeSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_has_response4(NativeSafeHandle pInProcessHandler, out bool isResponse4);
        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_trailer(NativeSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool replace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_reset_stream(NativeSafeHandle pInProcessHandler, ulong errorCode);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_known_header(NativeSafeHandle pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_authentication_information(NativeSafeHandle pInProcessHandler, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_set_startup_error_page_content(byte* content, int contentLength);

        public static void HttpPostCompletion(NativeSafeHandle pInProcessHandler, int cbBytes)
        {
            Validate(http_post_completion(pInProcessHandler, cbBytes));
        }

        public static void HttpSetCompletionStatus(NativeSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus)
        {
            Validate(http_set_completion_status(pInProcessHandler, rquestNotificationStatus));
        }

        public static unsafe void HttpRegisterCallbacks(NativeSafeHandle pInProcessApplication,
            delegate* unmanaged<IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> requestCallback,
            delegate* unmanaged<IntPtr, int> shutdownCallback,
            delegate* unmanaged<IntPtr, void> disconnectCallback,
            delegate* unmanaged<IntPtr, int, int, REQUEST_NOTIFICATION_STATUS> asyncCallback,
            delegate* unmanaged<IntPtr, void> requestsDrainedHandler,
            IntPtr pvRequestContext,
            IntPtr pvShutdownContext)
        {
            Validate(register_callbacks(pInProcessApplication, requestCallback, shutdownCallback, disconnectCallback, asyncCallback, requestsDrainedHandler, pvRequestContext, pvShutdownContext));
        }

        internal static unsafe int HttpWriteResponseBytes(NativeSafeHandle pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
        {
            return http_write_response_bytes(pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);
        }

        public static int HttpFlushResponseBytes(NativeSafeHandle pInProcessHandler, bool fMoreData, out bool fCompletionExpected)
        {
            return http_flush_response_bytes(pInProcessHandler, fMoreData, out fCompletionExpected);
        }

        internal static unsafe HttpApiTypes.HTTP_REQUEST_V2* HttpGetRawRequest(NativeSafeHandle pInProcessHandler)
        {
            return http_get_raw_request(pInProcessHandler);
        }

        public static void HttpStopCallsIntoManaged(NativeSafeHandle pInProcessApplication)
        {
            Validate(http_stop_calls_into_managed(pInProcessApplication));
        }

        public static void HttpStopIncomingRequests(NativeSafeHandle pInProcessApplication)
        {
            Validate(http_stop_incoming_requests(pInProcessApplication));
        }

        public static void HttpDisableBuffering(NativeSafeHandle pInProcessHandler)
        {
            Validate(http_disable_buffering(pInProcessHandler));
        }

        public static void HttpSetResponseStatusCode(NativeSafeHandle pInProcessHandler, ushort statusCode, string pszReason)
        {
            Validate(http_set_response_status_code(pInProcessHandler, statusCode, pszReason));
        }

        public static unsafe int HttpReadRequestBytes(NativeSafeHandle pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return http_read_request_bytes(pInProcessHandler, pvBuffer, cbBuffer, out dwBytesReceived, out fCompletionExpected);
        }

        public static void HttpGetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr)
        {
            http_get_completion_info(pCompletionInfo, out cbBytes, out hr);
        }

        public static void HttpSetManagedContext(NativeSafeHandle pInProcessHandler, IntPtr pvManagedContext)
        {
            Validate(http_set_managed_context(pInProcessHandler, pvManagedContext));
        }

        internal static IISConfigurationData HttpGetApplicationProperties()
        {
            var iisConfigurationData = new IISConfigurationData();
            Validate(http_get_application_properties(ref iisConfigurationData));
            return iisConfigurationData;
        }

        public static bool HttpTryGetServerVariable(NativeSafeHandle pInProcessHandler, string variableName, out string value)
        {
            return http_get_server_variable(pInProcessHandler, variableName, out value) == 0;
        }

        public static void HttpSetServerVariable(NativeSafeHandle pInProcessHandler, string variableName, string value)
        {
            Validate(http_set_server_variable(pInProcessHandler, variableName, value));
        }

        public static unsafe int HttpWebsocketsReadBytes(
            NativeSafeHandle pInProcessHandler,
            byte* pvBuffer,
            int cbBuffer,
            delegate* unmanaged<IntPtr, IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> pfnCompletionCallback,
            IntPtr pvCompletionContext, out int dwBytesReceived,
            out bool fCompletionExpected)
        {
            return http_websockets_read_bytes(pInProcessHandler, pvBuffer, cbBuffer, pfnCompletionCallback, pvCompletionContext, out dwBytesReceived, out fCompletionExpected);
        }

        internal static unsafe int HttpWebsocketsWriteBytes(
            NativeSafeHandle pInProcessHandler,
            HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks,
            int nChunks,
            delegate* unmanaged<IntPtr, IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out bool fCompletionExpected)
        {
            return http_websockets_write_bytes(pInProcessHandler, pDataChunks, nChunks, pfnCompletionCallback, pvCompletionContext, out fCompletionExpected);
        }

        public static void HttpEnableWebsockets(NativeSafeHandle pInProcessHandler)
        {
            Validate(http_enable_websockets(pInProcessHandler));
        }

        public static bool HttpTryCancelIO(NativeSafeHandle pInProcessHandler)
        {
            var hr = http_cancel_io(pInProcessHandler);
            // ERROR_NOT_FOUND is expected if async operation finished
            // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363792(v=vs.85).aspx
            // ERROR_INVALID_PARAMETER is expected for "fake" requests like applicationInitialization ones
            if (hr == ERROR_NOT_FOUND || hr == ERROR_INVALID_PARAMETER)
            {
                return false;
            }
            Validate(hr);
            return true;
        }

        public static void HttpCloseConnection(NativeSafeHandle pInProcessHandler)
        {
            Validate(http_close_connection(pInProcessHandler));
        }

        public static unsafe void HttpResponseSetUnknownHeader(NativeSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace)
        {
            Validate(http_response_set_unknown_header(pInProcessHandler, pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace));
        }

        public static unsafe void HttpResponseSetKnownHeader(NativeSafeHandle pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace)
        {
            Validate(http_response_set_known_header(pInProcessHandler, headerId, pHeaderValue, length, fReplace));
        }

        public static void HttpGetAuthenticationInformation(NativeSafeHandle pInProcessHandler, out string authType, out IntPtr token)
        {
            Validate(http_get_authentication_information(pInProcessHandler, out authType, out token));
        }

        internal static unsafe void HttpSetStartupErrorPageContent(byte[] content)
        {
            fixed (byte* bytePtr = content)
            {
                http_set_startup_error_page_content(bytePtr, content.Length);
            }
        }

        internal static unsafe void HttpResponseSetTrailer(NativeSafeHandle pInProcessHandler, byte* pHeaderName, byte* pHeaderValue, ushort length, bool replace)
        {
            Validate(http_response_set_trailer(pInProcessHandler, pHeaderName, pHeaderValue, length, false));
        }

        internal static unsafe void HttpResetStream(NativeSafeHandle pInProcessHandler, ulong errorCode)
        {
            Validate(http_reset_stream(pInProcessHandler, errorCode));
        }

        internal static unsafe bool HttpSupportTrailer(NativeSafeHandle pInProcessHandler)
        {
            bool supportsTrailers;
            Validate(http_has_response4(pInProcessHandler, out supportsTrailers));
            return supportsTrailers;
        }

        private static void Validate(int hr)
        {
            if (hr != HR_OK)
            {
                throw Marshal.GetExceptionForHR(hr)!;
            }
        }
    }
}
