// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.IIS.Core;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.IIS;

internal static partial class NativeMethods
{
    internal const int HR_OK = 0;
    internal const int ERROR_NOT_FOUND = unchecked((int)0x80070490);
    internal const int ERROR_OPERATION_ABORTED = unchecked((int)0x800703E3);
    internal const int ERROR_INVALID_PARAMETER = unchecked((int)0x80070057);
    internal const int ERROR_HANDLE_EOF = unchecked((int)0x80070026);

    private const string KERNEL32 = "kernel32.dll";

    internal const string AspNetCoreModuleDll = "aspnetcorev2_inprocess.dll";

    [LibraryImport(KERNEL32, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr handle);

    [LibraryImport(KERNEL32, EntryPoint = "GetModuleHandleW")]
    private static partial IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

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

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_post_completion(NativeSafeHandle pInProcessHandler, int cbBytes);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_set_completion_status(NativeSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS rquestNotificationStatus);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial void http_indicate_completion(NativeSafeHandle pInProcessHandler, REQUEST_NOTIFICATION_STATUS notificationStatus);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int register_callbacks(NativeSafeHandle pInProcessApplication,
        delegate* unmanaged<IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> requestCallback,
        delegate* unmanaged<IntPtr, int> shutdownCallback,
        delegate* unmanaged<IntPtr, void> disconnectCallback,
        delegate* unmanaged<IntPtr, int, int, REQUEST_NOTIFICATION_STATUS> asyncCallback,
        delegate* unmanaged<IntPtr, void> requestsDrainedHandler,
        IntPtr pvRequestContext,
        IntPtr pvShutdownContext);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_write_response_bytes(NativeSafeHandle pInProcessHandler, HTTP_DATA_CHUNK* pDataChunks, int nChunks, [MarshalAs(UnmanagedType.Bool)] out bool fCompletionExpected);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_flush_response_bytes(NativeSafeHandle pInProcessHandler, [MarshalAs(UnmanagedType.Bool)] bool fMoreData, [MarshalAs(UnmanagedType.Bool)] out bool fCompletionExpected);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial HTTP_REQUEST_V2* http_get_raw_request(NativeSafeHandle pInProcessHandler);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_stop_calls_into_managed(NativeSafeHandle pInProcessApplication);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_stop_incoming_requests(NativeSafeHandle pInProcessApplication);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_disable_buffering(NativeSafeHandle pInProcessHandler);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_set_response_status_code(NativeSafeHandle pInProcessHandler, ushort statusCode, [MarshalAs(UnmanagedType.LPStr)] string pszReason);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_read_request_bytes(NativeSafeHandle pInProcessHandler, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, [MarshalAs(UnmanagedType.Bool)] out bool fCompletionExpected);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial void http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_set_managed_context(NativeSafeHandle pInProcessHandler, IntPtr pvManagedContext);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_get_application_properties(out IISConfigurationData iiConfigData);

    [LibraryImport(AspNetCoreModuleDll)]
    [SuppressMessage("LibraryImportGenerator", "SYSLIB1051:Specified type is not supported by source-generated P/Invokes", Justification = "The enum is handled by the runtime.")]
    private static unsafe partial int http_query_request_property(
        ulong requestId,
        HTTP_REQUEST_PROPERTY propertyId,
        void* qualifier,
        uint qualifierSize,
        void* output,
        uint outputSize,
        uint* bytesReturned,
        IntPtr overlapped);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_get_server_variable(
        NativeSafeHandle pInProcessHandler,
        [MarshalAs(UnmanagedType.LPStr)] string variableName,
        [MarshalAs(UnmanagedType.BStr)] out string value);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_set_server_variable(
        NativeSafeHandle pInProcessHandler,
        [MarshalAs(UnmanagedType.LPStr)] string variableName,
        [MarshalAs(UnmanagedType.LPWStr)] string value);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_websockets_read_bytes(
        NativeSafeHandle pInProcessHandler,
        byte* pvBuffer,
        int cbBuffer,
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> pfnCompletionCallback,
        IntPtr pvCompletionContext,
        out int dwBytesReceived,
        [MarshalAs(UnmanagedType.Bool)] out bool fCompletionExpected);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_websockets_write_bytes(
        NativeSafeHandle pInProcessHandler,
        HTTP_DATA_CHUNK* pDataChunks,
        int nChunks,
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, REQUEST_NOTIFICATION_STATUS> pfnCompletionCallback,
        IntPtr pvCompletionContext,
        [MarshalAs(UnmanagedType.Bool)] out bool fCompletionExpected);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_enable_websockets(NativeSafeHandle pInProcessHandler);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_cancel_io(NativeSafeHandle pInProcessHandler);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_close_connection(NativeSafeHandle pInProcessHandler);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_response_set_need_goaway(NativeSafeHandle pInProcessHandler);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_response_set_unknown_header(NativeSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, [MarshalAs(UnmanagedType.Bool)] bool fReplace);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_has_response4(NativeSafeHandle pInProcessHandler, [MarshalAs(UnmanagedType.Bool)] out bool isResponse4);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_response_set_trailer(NativeSafeHandle pInProcessHandler, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, [MarshalAs(UnmanagedType.Bool)] bool replace);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_reset_stream(NativeSafeHandle pInProcessHandler, ulong errorCode);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_response_set_known_header(NativeSafeHandle pInProcessHandler, int headerId, byte* pHeaderValue, ushort length, [MarshalAs(UnmanagedType.Bool)] bool fReplace);

    [LibraryImport(AspNetCoreModuleDll)]
    private static partial int http_get_authentication_information(NativeSafeHandle pInProcessHandler, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);

    [LibraryImport(AspNetCoreModuleDll)]
    private static unsafe partial int http_set_startup_error_page_content(byte* content, int contentLength);

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

    internal static unsafe int HttpWriteResponseBytes(NativeSafeHandle pInProcessHandler, HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
    {
        return http_write_response_bytes(pInProcessHandler, pDataChunks, nChunks, out fCompletionExpected);
    }

    public static int HttpFlushResponseBytes(NativeSafeHandle pInProcessHandler, bool fMoreData, out bool fCompletionExpected)
    {
        return http_flush_response_bytes(pInProcessHandler, fMoreData, out fCompletionExpected);
    }

    internal static unsafe HTTP_REQUEST_V2* HttpGetRawRequest(NativeSafeHandle pInProcessHandler)
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
        Validate(http_get_application_properties(out IISConfigurationData iisConfigurationData));
        return iisConfigurationData;
    }

    public static unsafe int HttpQueryRequestProperty(ulong requestId, HTTP_REQUEST_PROPERTY propertyId, void* qualifier, uint qualifierSize, void* output, uint outputSize, uint* bytesReturned, IntPtr overlapped)
    {
        return http_query_request_property(requestId, propertyId, qualifier, qualifierSize, output, outputSize, bytesReturned, overlapped);
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
        HTTP_DATA_CHUNK* pDataChunks,
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

    internal static void HttpSetNeedGoAway(NativeSafeHandle pInProcessHandler)
    {
        Validate(http_response_set_need_goaway(pInProcessHandler));
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
        Validate(http_response_set_trailer(pInProcessHandler, pHeaderName, pHeaderValue, length, replace));
    }

    internal static unsafe void HttpResetStream(NativeSafeHandle pInProcessHandler, ulong errorCode)
    {
        Validate(http_reset_stream(pInProcessHandler, errorCode));
    }

    internal static unsafe bool HttpHasResponse4(NativeSafeHandle pInProcessHandler)
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
