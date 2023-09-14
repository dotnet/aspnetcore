// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using Windows.Win32;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static partial class HttpApi
{
    private const string HTTPAPI = "httpapi.dll";

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveRequestEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, IntPtr pEntityBuffer, uint entityBufferLength, out uint bytesReturned, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, Windows.Win32.Networking.HttpServer.HTTP_SSL_CLIENT_CERT_INFO* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpReceiveHttpRequest(SafeHandle requestQueueHandle, ulong requestId, uint flags, Windows.Win32.Networking.HttpServer.HTTP_REQUEST_V1* pRequestBuffer, uint requestBufferLength, uint* pBytesReturned, NativeOverlapped* pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpSendHttpResponse(SafeHandle requestQueueHandle, ulong requestId, uint flags, Windows.Win32.Networking.HttpServer.HTTP_RESPONSE_V2* pHttpResponse, Windows.Win32.Networking.HttpServer.HTTP_CACHE_POLICY* pCachePolicy, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpWaitForDisconnectEx(SafeHandle requestQueueHandle, ulong connectionId, uint reserved, NativeOverlapped* overlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpSendResponseEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, ushort entityChunkCount, Windows.Win32.Networking.HttpServer.HTTP_DATA_CHUNK* pEntityChunks, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

    [SuppressMessage("LibraryImportGenerator", "SYSLIB1051:Specified type is not supported by source-generated P/Invokes", Justification = "The enum is handled by the runtime.")]
    [LibraryImport(HTTPAPI, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint HttpCreateRequestQueue(Windows.Win32.Networking.HttpServer.HTTPAPI_VERSION version, string? pName, IntPtr pSecurityAttributes, uint flags, out HttpRequestQueueV2Handle pReqQueueHandle);

    internal unsafe delegate uint HttpGetRequestPropertyInvoker(SafeHandle requestQueueHandle, ulong requestId, HTTP_REQUEST_PROPERTY propertyId,
        void* qualifier, uint qualifierSize, void* output, uint outputSize, uint* bytesReturned, IntPtr overlapped);

    internal unsafe delegate uint HttpSetRequestPropertyInvoker(SafeHandle requestQueueHandle, ulong requestId, HTTP_REQUEST_PROPERTY propertyId, void* input, uint inputSize, IntPtr overlapped);

    // HTTP_PROPERTY_FLAGS.Present (1)
    internal static HTTP_PROPERTY_FLAGS HTTP_PROPERTY_FLAGS_PRESENT { get; } = new() { _bitfield = 0x00000001 };
    // This property is used by HttpListener to pass the version structure to the native layer in API calls.
    internal static HTTPAPI_VERSION Version { get; } = new () { HttpApiMajorVersion = 2 };
    internal static SafeLibraryHandle? HttpApiModule { get; }
    internal static HttpGetRequestPropertyInvoker? HttpGetRequestProperty { get; }
    internal static HttpSetRequestPropertyInvoker? HttpSetRequestProperty { get; }
    [MemberNotNullWhen(true, nameof(HttpSetRequestProperty))]
    internal static bool SupportsTrailers { get; }
    [MemberNotNullWhen(true, nameof(HttpSetRequestProperty))]
    internal static bool SupportsReset { get; }
    internal static bool SupportsDelegation { get; }
    internal static bool Supported { get; }

    unsafe static HttpApi()
    {
        var statusCode = PInvoke.HttpInitialize(Version, HTTP_INITIALIZE.HTTP_INITIALIZE_SERVER | HTTP_INITIALIZE.HTTP_INITIALIZE_CONFIG);

        if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
        {
            Supported = true;
            HttpApiModule = SafeLibraryHandle.Open(HTTPAPI);
            HttpGetRequestProperty = HttpApiModule.GetProcAddress<HttpGetRequestPropertyInvoker>("HttpQueryRequestProperty", throwIfNotFound: false);
            HttpSetRequestProperty = HttpApiModule.GetProcAddress<HttpSetRequestPropertyInvoker>("HttpSetRequestProperty", throwIfNotFound: false);
            SupportsReset = HttpSetRequestProperty != null;
            SupportsTrailers = IsFeatureSupported(HTTP_FEATURE_ID.HttpFeatureResponseTrailers);
            SupportsDelegation = IsFeatureSupported(HTTP_FEATURE_ID.HttpFeatureDelegateEx);
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
