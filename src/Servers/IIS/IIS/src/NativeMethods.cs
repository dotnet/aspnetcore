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
        internal const int COR_E_IO = unchecked((int)0x80131620);

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

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_post_completion(IntPtr pInProcessHandler, int cbBytes);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_completion_status(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_indicate_completion(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS notificationStatus);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int register_callbacks(IntPtr pInProcessApplication,
            PFN_REQUEST_HANDLER requestCallback,
            PFN_SHUTDOWN_HANDLER shutdownCallback,
            PFN_DISCONNECT_HANDLER disconnectCallback,
            PFN_ASYNC_COMPLETION asyncCallback,
            IntPtr pvRequestContext,
            IntPtr pvShutdownContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_write_response_bytes(IntPtr pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_flush_response_bytes(IntPtr pInProcessHandler, bool fMoreData, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe HttpApiTypes.HTTP_REQUEST_V2* http_get_raw_request(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_stop_calls_into_managed(IntPtr pInProcessApplication);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_stop_incoming_requests(IntPtr pInProcessApplication);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_disable_buffering(IntPtr pInProcessApplication);

        [DllImport(AspNetCoreModuleDll, CharSet = CharSet.Ansi)]
        private static extern int http_set_response_status_code(IntPtr pInProcessHandler, ushort statusCode, string pszReason);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_read_request_bytes(IntPtr pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern void http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_managed_context(IntPtr pInProcessHandler, IntPtr pvManagedContext);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_application_properties(ref IISConfigurationData iiConfigData);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_server_variable(
            IntPtr pInProcessHandler,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            [MarshalAs(UnmanagedType.BStr)] out string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_set_server_variable(
            IntPtr pInProcessHandler,
            [MarshalAs(UnmanagedType.LPStr)] string variableName,
            [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_read_bytes(
            IntPtr pInProcessHandler,
            byte* pvBuffer,
            int cbBuffer,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out int dwBytesReceived,
            out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_websockets_write_bytes(
            IntPtr pInProcessHandler,
            HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks,
            int nChunks,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out bool fCompletionExpected);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_enable_websockets(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_cancel_io(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_close_connection(IntPtr pInProcessHandler);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_unknown_header(IntPtr pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern unsafe int http_response_set_known_header(IntPtr pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace);

        [DllImport(AspNetCoreModuleDll)]
        private static extern int http_get_authentication_information(IntPtr pInProcessHandler, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);

        public static void HttpPostCompletion(IntPtr pInProcessHandler, int cbBytes)
        {
            Validate(http_post_completion(pInProcessHandler, cbBytes));
        }

        public static void HttpSetCompletionStatus(IntPtr pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus)
        {
            Validate(http_set_completion_status(pInProcessHandler, rquestNotificationStatus));
        }

        public static void HttpRegisterCallbacks(IntPtr pInProcessApplication,
            PFN_REQUEST_HANDLER requestCallback,
            PFN_SHUTDOWN_HANDLER shutdownCallback,
            PFN_DISCONNECT_HANDLER disconnectCallback,
            PFN_ASYNC_COMPLETION asyncCallback,
            IntPtr pvRequestContext,
            IntPtr pvShutdownContext)
        {
            Validate(register_callbacks(pInProcessApplication, requestCallback, shutdownCallback, disconnectCallback, asyncCallback, pvRequestContext, pvShutdownContext));
        }

        public static unsafe int HttpWriteResponseBytes(IntPtr pInProcessHandler, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
        {
            return http_write_response_bytes(pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);
        }

        public static int HttpFlushResponseBytes(IntPtr pInProcessHandler, bool fMoreData, out bool fCompletionExpected)
        {
            return http_flush_response_bytes(pInProcessHandler, fMoreData, out fCompletionExpected);
        }

        public static unsafe HttpApiTypes.HTTP_REQUEST_V2* HttpGetRawRequest(IntPtr pInProcessHandler)
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

        public static void HttpDisableBuffering(IntPtr pInProcessApplication)
        {
            Validate(http_disable_buffering(pInProcessApplication));
        }

        public static void HttpSetResponseStatusCode(IntPtr pInProcessHandler, ushort statusCode, string pszReason)
        {
            Validate(http_set_response_status_code(pInProcessHandler, statusCode, pszReason));
        }

        public static unsafe int HttpReadRequestBytes(IntPtr pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return http_read_request_bytes(pInProcessHandler, pvBuffer, cbBuffer, out dwBytesReceived, out fCompletionExpected);
        }

        public static void HttpGetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr)
        {
            http_get_completion_info(pCompletionInfo, out cbBytes, out hr);
        }

        public static void HttpSetManagedContext(IntPtr pInProcessHandler, IntPtr pvManagedContext)
        {
            Validate(http_set_managed_context(pInProcessHandler, pvManagedContext));
        }

        public static IISConfigurationData HttpGetApplicationProperties()
        {
            var iisConfigurationData = new IISConfigurationData();
            Validate(http_get_application_properties(ref iisConfigurationData));
            return iisConfigurationData;
        }

        public static bool HttpTryGetServerVariable(IntPtr pInProcessHandler, string variableName, out string value)
        {
            return http_get_server_variable(pInProcessHandler, variableName, out value) == 0;
        }

        public static void HttpSetServerVariable(IntPtr pInProcessHandler, string variableName, string value)
        {
            Validate(http_set_server_variable(pInProcessHandler, variableName, value));
        }

        public static unsafe int HttpWebsocketsReadBytes(
            IntPtr pInProcessHandler,
            byte* pvBuffer,
            int cbBuffer,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext, out int dwBytesReceived,
            out bool fCompletionExpected)
        {
            return http_websockets_read_bytes(pInProcessHandler, pvBuffer, cbBuffer, pfnCompletionCallback, pvCompletionContext, out dwBytesReceived, out fCompletionExpected);
        }

        public static unsafe int HttpWebsocketsWriteBytes(
            IntPtr pInProcessHandler,
            HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks,
            int nChunks,
            PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback,
            IntPtr pvCompletionContext,
            out bool fCompletionExpected)
        {
            return http_websockets_write_bytes(pInProcessHandler, pDataChunks, nChunks, pfnCompletionCallback, pvCompletionContext, out fCompletionExpected);
        }

        public static void HttpEnableWebsockets(IntPtr pInProcessHandler)
        {
            Validate(http_enable_websockets(pInProcessHandler));
        }

        public static bool HttpTryCancelIO(IntPtr pInProcessHandler)
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

        public static void HttpCloseConnection(IntPtr pInProcessHandler)
        {
            Validate(http_close_connection(pInProcessHandler));
        }

        public static unsafe void HttpResponseSetUnknownHeader(IntPtr pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace)
        {
            Validate(http_response_set_unknown_header(pInProcessHandler, pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace));
        }

        public static unsafe void HttpResponseSetKnownHeader(IntPtr pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, bool fReplace)
        {
            Validate(http_response_set_known_header(pInProcessHandler, headerId, pHeaderValue, length, fReplace));
        }

        public static void HttpGetAuthenticationInformation(IntPtr pInProcessHandler, out string authType, out IntPtr token)
        {
            Validate(http_get_authentication_information(pInProcessHandler, out authType, out token));
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
