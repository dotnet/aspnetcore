// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using static Microsoft.AspNetCore.HttpSys.Internal.HttpApiTypes;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static unsafe class HttpApi
    {
        private const string HTTPAPI = "httpapi.dll";

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpInitialize(HTTPAPI_VERSION version, uint flags, void* pReserved);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpReceiveRequestEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, IntPtr pEntityBuffer, uint entityBufferLength, out uint bytesReturned, SafeNativeOverlapped pOverlapped);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, HTTP_SSL_CLIENT_CERT_INFO* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpReceiveClientCertificate(SafeHandle requestQueueHandle, ulong connectionId, uint flags, byte* pSslClientCertInfo, uint sslClientCertInfoSize, uint* pBytesReceived, SafeNativeOverlapped pOverlapped);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpReceiveHttpRequest(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_REQUEST* pRequestBuffer, uint requestBufferLength, uint* pBytesReturned, SafeNativeOverlapped pOverlapped);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpSendHttpResponse(SafeHandle requestQueueHandle, ulong requestId, uint flags, HTTP_RESPONSE_V2* pHttpResponse, HTTP_CACHE_POLICY* pCachePolicy, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpSendResponseEntityBody(SafeHandle requestQueueHandle, ulong requestId, uint flags, ushort entityChunkCount, HTTP_DATA_CHUNK* pEntityChunks, uint* pBytesSent, IntPtr pReserved1, uint Reserved2, SafeNativeOverlapped pOverlapped, IntPtr pLogData);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpCancelHttpRequest(SafeHandle requestQueueHandle, ulong requestId, IntPtr pOverlapped);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpWaitForDisconnectEx(SafeHandle requestQueueHandle, ulong connectionId, uint reserved, SafeNativeOverlapped overlapped);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpCreateServerSession(HTTPAPI_VERSION version, ulong* serverSessionId, uint reserved);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpCreateUrlGroup(ulong serverSessionId, ulong* urlGroupId, uint reserved);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint HttpAddUrlToUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, ulong context, uint pReserved);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpSetUrlGroupProperty(ulong urlGroupId, HTTP_SERVER_PROPERTY serverProperty, IntPtr pPropertyInfo, uint propertyInfoLength);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint HttpRemoveUrlFromUrlGroup(ulong urlGroupId, string pFullyQualifiedUrl, uint flags);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpCloseServerSession(ulong serverSessionId);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpCloseUrlGroup(ulong urlGroupId);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern uint HttpSetRequestQueueProperty(SafeHandle requestQueueHandle, HTTP_SERVER_PROPERTY serverProperty, IntPtr pPropertyInfo, uint propertyInfoLength, uint reserved, IntPtr pReserved);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe uint HttpCreateRequestQueue(HTTPAPI_VERSION version, string pName,
            UnsafeNclNativeMethods.SECURITY_ATTRIBUTES pSecurityAttributes, HTTP_CREATE_REQUEST_QUEUE_FLAG flags, out HttpRequestQueueV2Handle pReqQueueHandle);

        [DllImport(HTTPAPI, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern unsafe uint HttpCloseRequestQueue(IntPtr pReqQueueHandle);

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

        internal static SafeLibraryHandle HttpApiModule { get; private set; }
        internal static HttpSetRequestPropertyInvoker HttpSetRequestProperty { get; private set; }
        internal static bool SupportsTrailers { get; private set; }
        internal static bool SupportsReset { get; private set; }

        static HttpApi()
        {
            InitHttpApi(2, 0);
        }

        private static void InitHttpApi(ushort majorVersion, ushort minorVersion)
        {
            version.HttpApiMajorVersion = majorVersion;
            version.HttpApiMinorVersion = minorVersion;

            var statusCode = HttpInitialize(version, (uint)HTTP_FLAGS.HTTP_INITIALIZE_SERVER, null);

            supported = statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS;

            if (supported)
            {
                HttpApiModule = SafeLibraryHandle.Open(HTTPAPI);
                HttpSetRequestProperty = HttpApiModule.GetProcAddress<HttpSetRequestPropertyInvoker>("HttpSetRequestProperty", throwIfNotFound: false);

                SupportsReset = HttpSetRequestProperty != null;
                // Trailers support was added in the same release as Reset, but there's no method we can export to check it directly.
                SupportsTrailers = SupportsReset;
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
    }
}
