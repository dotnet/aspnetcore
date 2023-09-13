// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using Windows.Win32;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static unsafe partial class HttpApi
{
    private const string HTTPAPI = "httpapi.dll";

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveRequestEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, IntPtr pEntityBuffer, uint entityBufferLength, out uint bytesReturned, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, HttpApiTypes.HTTP_SSL_CLIENT_CERT_INFO* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, byte* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveHttpRequest(SafeHandle requestQueueHandle, ulong requestId, uint flags, Windows.Win32.Networking.HttpServer.HTTP_REQUEST_V1* pRequestBuffer, uint requestBufferLength, uint* pBytesReturned, NativeOverlapped* pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpSendHttpResponse(SafeHandle requestQueueHandle, ulong requestId, uint flags, HttpApiTypes.HTTP_RESPONSE_V2* pHttpResponse, HttpApiTypes.HTTP_CACHE_POLICY* pCachePolicy, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpWaitForDisconnectEx(SafeHandle requestQueueHandle, ulong connectionId, uint reserved, NativeOverlapped* overlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpSendResponseEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, ushort entityChunkCount, HttpApiTypes.HTTP_DATA_CHUNK* pEntityChunks, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

    [LibraryImport(HTTPAPI, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static unsafe partial uint HttpCreateRequestQueue(HttpApiTypes.HTTPAPI_VERSION version, string? pName,
        IntPtr pSecurityAttributes, HttpApiTypes.HTTP_CREATE_REQUEST_QUEUE_FLAG flags, out HttpRequestQueueV2Handle pReqQueueHandle);

    internal delegate uint HttpGetRequestPropertyInvoker(SafeHandle requestQueueHandle, ulong requestId, HTTP_REQUEST_PROPERTY propertyId,
        void* qualifier, uint qualifierSize, void* output, uint outputSize, uint* bytesReturned, IntPtr overlapped);

    internal delegate uint HttpSetRequestPropertyInvoker(SafeHandle requestQueueHandle, ulong requestId, HTTP_REQUEST_PROPERTY propertyId, void* input, uint inputSize, IntPtr overlapped);

    private static HttpApiTypes.HTTPAPI_VERSION oldVersion;
    private static HTTPAPI_VERSION version;

    // This property is used by HttpListener to pass the version structure to the native layer in API
    // calls.

    internal static HttpApiTypes.HTTPAPI_VERSION OldVersion => oldVersion;

    internal static HTTPAPI_VERSION Version => version;

    internal static SafeLibraryHandle? HttpApiModule { get; private set; }
    internal static HttpGetRequestPropertyInvoker? HttpGetRequestProperty { get; private set; }
    internal static HttpSetRequestPropertyInvoker? HttpSetRequestProperty { get; private set; }
    [MemberNotNullWhen(true, nameof(HttpSetRequestProperty))]
    internal static bool SupportsTrailers { get; private set; }
    [MemberNotNullWhen(true, nameof(HttpSetRequestProperty))]
    internal static bool SupportsReset { get; private set; }
    internal static bool SupportsDelegation { get; private set; }

    static HttpApi()
    {
        InitHttpApi();
    }

    private static void InitHttpApi()
    {
        oldVersion.HttpApiMajorVersion = 2;
        oldVersion.HttpApiMinorVersion = 0;
        version.HttpApiMajorVersion = 2;
        version.HttpApiMinorVersion = 0;

        var statusCode = PInvoke.HttpInitialize(version, HTTP_INITIALIZE.HTTP_INITIALIZE_SERVER | HTTP_INITIALIZE.HTTP_INITIALIZE_CONFIG, null);

        supported = statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;

        if (supported)
        {
            HttpApiModule = SafeLibraryHandle.Open(HTTPAPI);
            HttpGetRequestProperty = HttpApiModule.GetProcAddress<HttpGetRequestPropertyInvoker>("HttpQueryRequestProperty", throwIfNotFound: false);
            HttpSetRequestProperty = HttpApiModule.GetProcAddress<HttpSetRequestPropertyInvoker>("HttpSetRequestProperty", throwIfNotFound: false);
            SupportsReset = HttpSetRequestProperty != null;
            SupportsTrailers = IsFeatureSupported(HTTP_FEATURE_ID.HttpFeatureResponseTrailers);
            SupportsDelegation = IsFeatureSupported(HTTP_FEATURE_ID.HttpFeatureDelegateEx);
        }
    }

    private static volatile bool supported;
    internal static bool Supported
    {
        get
        {
            return supported;
        }
    }

    private static bool IsFeatureSupported(HTTP_FEATURE_ID feature)
    {
        try
        {
            return PInvoke.HttpIsFeatureSupported(feature);
        }
        catch (EntryPointNotFoundException) { }

        return false;
    }
}
