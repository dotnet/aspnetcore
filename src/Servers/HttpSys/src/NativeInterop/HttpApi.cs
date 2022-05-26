// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using static Microsoft.AspNetCore.HttpSys.Internal.HttpApiTypes;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal static unsafe partial class HttpApi
{
    private const string HTTPAPI = "httpapi.dll";

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpInitialize(HTTPAPI_VERSION version, uint flags, void* pReserved);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveRequestEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, IntPtr pEntityBuffer, uint entityBufferLength, out uint bytesReturned, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, HTTP_SSL_CLIENT_CERT_INFO* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, byte* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpReceiveHttpRequest(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_REQUEST* pRequestBuffer, uint requestBufferLength, uint* pBytesReturned, NativeOverlapped* pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpSendHttpResponse(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_RESPONSE_V2* pHttpResponse, HTTP_CACHE_POLICY* pCachePolicy, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpSendResponseEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, ushort entityChunkCount, HTTP_DATA_CHUNK* pEntityChunks, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpCancelHttpRequest(SafeHandle requestQueueHandle, ulong requestId, IntPtr pOverlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpWaitForDisconnectEx(SafeHandle requestQueueHandle, ulong connectionId, uint reserved, NativeOverlapped* overlapped);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpCreateServerSession(HTTPAPI_VERSION version, ulong* serverSessionId, uint reserved);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpCreateUrlGroup(ulong serverSessionId, ulong* urlGroupId, uint reserved);

    [LibraryImport(HTTPAPI, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint HttpFindUrlGroupId(string pFullyQualifiedUrl, SafeHandle requestQueueHandle, ulong* urlGroupId);

    [LibraryImport(HTTPAPI, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint HttpAddUrlToUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, ulong context, uint pReserved);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpSetUrlGroupProperty(ulong urlGroupId, HTTP_SERVER_PROPERTY serverProperty, IntPtr pPropertyInfo, uint propertyInfoLength);

    [LibraryImport(HTTPAPI, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint HttpRemoveUrlFromUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, uint flags);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpCloseServerSession(ulong serverSessionId);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpCloseUrlGroup(ulong urlGroupId);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static partial uint HttpSetRequestQueueProperty(SafeHandle requestQueueHandle, HTTP_SERVER_PROPERTY serverProperty, IntPtr pPropertyInfo, uint propertyInfoLength, uint reserved, IntPtr pReserved);

    [LibraryImport(HTTPAPI, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static unsafe partial uint HttpCreateRequestQueue(HTTPAPI_VERSION version, string? pName,
        IntPtr pSecurityAttributes, HTTP_CREATE_REQUEST_QUEUE_FLAG flags, out HttpRequestQueueV2Handle pReqQueueHandle);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpCloseRequestQueue(IntPtr pReqQueueHandle);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool HttpIsFeatureSupported(HTTP_FEATURE_ID feature);

    [LibraryImport(HTTPAPI, SetLastError = true)]
    internal static unsafe partial uint HttpDelegateRequestEx(SafeHandle pReqQueueHandle, SafeHandle pDelegateQueueHandle, ulong requestId,
        ulong delegateUrlGroupId, ulong propertyInfoSetSize, HTTP_DELEGATE_REQUEST_PROPERTY_INFO* pRequestPropertyBuffer);

    internal delegate uint HttpSetRequestPropertyInvoker(SafeHandle requestQueueHandle, ulong requestId, HTTP_REQUEST_PROPERTY propertyId, void* input, uint inputSize, IntPtr overlapped);

    private static HTTPAPI_VERSION version;

    // This property is used by HttpListener to pass the version structure to the native layer in API
    // calls.

    internal static HTTPAPI_VERSION Version
    {
        get
        {
            return version;
        }
    }

    // This property is used by HttpListener to get the Api version in use so that it uses appropriate
    // Http APIs.

    internal static HTTP_API_VERSION ApiVersion
    {
        get
        {
            if (version.HttpApiMajorVersion == 2 && version.HttpApiMinorVersion == 0)
            {
                return HTTP_API_VERSION.Version20;
            }
            else if (version.HttpApiMajorVersion == 1 && version.HttpApiMinorVersion == 0)
            {
                return HTTP_API_VERSION.Version10;
            }
            else
            {
                return HTTP_API_VERSION.Invalid;
            }
        }
    }

    internal static SafeLibraryHandle? HttpApiModule { get; private set; }
    internal static HttpSetRequestPropertyInvoker? HttpSetRequestProperty { get; private set; }
    [MemberNotNullWhen(true, nameof(HttpSetRequestProperty))]
    internal static bool SupportsTrailers { get; private set; }
    [MemberNotNullWhen(true, nameof(HttpSetRequestProperty))]
    internal static bool SupportsReset { get; private set; }
    internal static bool SupportsDelegation { get; private set; }

    static HttpApi()
    {
        InitHttpApi(2, 0);
    }

    private static void InitHttpApi(ushort majorVersion, ushort minorVersion)
    {
        version.HttpApiMajorVersion = majorVersion;
        version.HttpApiMinorVersion = minorVersion;

        var statusCode = HttpInitialize(version, (uint)(HTTP_FLAGS.HTTP_INITIALIZE_SERVER | HTTP_FLAGS.HTTP_INITIALIZE_CONFIG), null);

        supported = statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;

        if (supported)
        {
            HttpApiModule = SafeLibraryHandle.Open(HTTPAPI);
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
            return HttpIsFeatureSupported(feature);
        }
        catch (EntryPointNotFoundException) { }

        return false;
    }
}
