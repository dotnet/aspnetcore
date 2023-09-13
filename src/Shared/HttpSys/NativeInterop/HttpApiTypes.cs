
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Authentication;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.HttpSys.Internal;
#pragma warning disable IDE0044 // Add readonly modifier. We don't want to modify these interop types

internal static unsafe class HttpApiTypes
{
    internal enum HTTP_REQUEST_PROPERTY
    {
        HttpRequestPropertyIsb,
        HttpRequestPropertyTcpInfoV0,
        HttpRequestPropertyQuicStats,
        HttpRequestPropertyTcpInfoV1,
        HttpRequestPropertySni,
        HttpRequestPropertyStreamError,
    }

    internal enum HTTP_TIMEOUT_TYPE
    {
        EntityBody,
        DrainEntityBody,
        RequestQueue,
        IdleConnection,
        HeaderWait,
        MinSendRate,
    }

    internal struct HTTP_REQUEST_PROPERTY_STREAM_ERROR
    {
        internal uint ErrorCode;
    }

    private const int HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH = 255;
    internal const int SniPropertySizeInBytes = (sizeof(ushort) * (HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH + 1)) + sizeof(uint);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = SniPropertySizeInBytes)]
    internal struct HTTP_REQUEST_PROPERTY_SNI
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH + 1)]
        internal string Hostname;

        internal HTTP_REQUEST_PROPERTY_SNI_FLAGS Flags;
    }

    [Flags]
    internal enum HTTP_REQUEST_PROPERTY_SNI_FLAGS : uint
    {
        // Indicates that SNI was used for successful endpoint lookup during handshake.
        // If client sent the SNI but Http.sys still decided to use IP endpoint binding then this flag will not be set.
        HTTP_REQUEST_PROPERTY_SNI_FLAG_SNI_USED = 0x00000001,
        // Indicates that client did not send the SNI.
        HTTP_REQUEST_PROPERTY_SNI_FLAG_NO_SNI = 0x00000002,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct HTTP_DATA_CHUNK
    {
        [FieldOffset(0)]
        internal HTTP_DATA_CHUNK_TYPE DataChunkType;

        [FieldOffset(8)]
        internal FromMemory fromMemory;

        [FieldOffset(8)]
        internal FromFileHandle fromFile;

        [FieldOffset(8)]
        internal Trailers trailers;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FromMemory
    {
        // 4 bytes for 32bit, 8 bytes for 64bit
        internal IntPtr pBuffer;
        internal uint BufferLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FromFileHandle
    {
        internal ulong offset;
        internal ulong count;
        internal IntPtr fileHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Trailers
    {
        internal ushort trailerCount;
        internal IntPtr pTrailers;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTPAPI_VERSION
    {
        internal ushort HttpApiMajorVersion;
        internal ushort HttpApiMinorVersion;
    }

    // Only cache unauthorized GETs + HEADs.
    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_CACHE_POLICY
    {
        internal HTTP_CACHE_POLICY_TYPE Policy;
        internal uint SecondsToLive;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_SSL_CLIENT_CERT_INFO
    {
        internal uint CertFlags;
        internal uint CertEncodedSize;
        internal byte* pCertEncoded;
        internal void* Token;
        internal byte CertDeniedByMapper;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_RESPONSE
    {
        internal uint Flags;
        internal HTTP_VERSION Version;
        internal ushort StatusCode;
        internal ushort ReasonLength;
        internal byte* pReason;
        internal HTTP_RESPONSE_HEADERS Headers;
        internal ushort EntityChunkCount;
        internal HTTP_DATA_CHUNK* pEntityChunks;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_RESPONSE_V2
    {
        internal HTTP_RESPONSE Response_V1;
        internal ushort ResponseInfoCount;
        internal HTTP_RESPONSE_INFO* pResponseInfo;
    }

    internal enum HTTP_RESPONSE_INFO_FLAGS : uint
    {
        None = 0,
        PreserveOrder = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_MULTIPLE_KNOWN_HEADERS
    {
        internal HTTP_RESPONSE_HEADER_ID.Enum HeaderId;
        internal HTTP_RESPONSE_INFO_FLAGS Flags;
        internal ushort KnownHeaderCount;
        internal HTTP_KNOWN_HEADER* KnownHeaders;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_REQUEST_AUTH_INFO
    {
        internal HTTP_AUTH_STATUS AuthStatus;
        internal uint SecStatus;
        internal uint Flags;
        internal HTTP_REQUEST_AUTH_TYPE AuthType;
        internal IntPtr AccessToken;
        internal uint ContextAttributes;
        internal uint PackedContextLength;
        internal uint PackedContextType;
        internal IntPtr PackedContext;
        internal uint MutualAuthDataLength;
        internal char* pMutualAuthData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_SSL_PROTOCOL_INFO
    {
        internal SslProtocols Protocol;
        internal CipherAlgorithmType CipherType;
        internal uint CipherStrength;
        internal HashAlgorithmType HashType;
        internal uint HashStrength;
        internal ExchangeAlgorithmType KeyExchangeType;
        internal uint KeyExchangeStrength;
    }

    [Flags]
    internal enum HTTP_REQUEST_FLAGS
    {
        None = 0,
        MoreEntityBodyExists = 1,
        IPRouted = 2,
        Http2 = 4,
        Http3 = 8,
    }

    internal enum HTTP_AUTH_STATUS
    {
        HttpAuthStatusSuccess,
        HttpAuthStatusNotAuthenticated,
        HttpAuthStatusFailure,
    }

    internal enum HTTP_REQUEST_AUTH_TYPE
    {
        HttpRequestAuthTypeNone = 0,
        HttpRequestAuthTypeBasic,
        HttpRequestAuthTypeDigest,
        HttpRequestAuthTypeNTLM,
        HttpRequestAuthTypeNegotiate,
        HttpRequestAuthTypeKerberos
    }

    internal enum HTTP_QOS_SETTING_TYPE
    {
        HttpQosSettingTypeBandwidth,
        HttpQosSettingTypeConnectionLimit,
        HttpQosSettingTypeFlowRate
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_SERVER_AUTHENTICATION_INFO
    {
        internal HTTP_FLAGS Flags;
        internal HTTP_AUTH_TYPES AuthSchemes;
        internal bool ReceiveMutualAuth;
        internal bool ReceiveContextHandle;
        internal bool DisableNTLMCredentialCaching;
        internal ulong ExFlags;
        HTTP_SERVER_AUTHENTICATION_DIGEST_PARAMS DigestParams;
        HTTP_SERVER_AUTHENTICATION_BASIC_PARAMS BasicParams;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_SERVER_AUTHENTICATION_DIGEST_PARAMS
    {
        internal ushort DomainNameLength;
        internal char* DomainName;
        internal ushort RealmLength;
        internal char* Realm;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_SERVER_AUTHENTICATION_BASIC_PARAMS
    {
        ushort RealmLength;
        char* Realm;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_TIMEOUT_LIMIT_INFO
    {
        internal HTTP_FLAGS Flags;
        internal ushort EntityBody;
        internal ushort DrainEntityBody;
        internal ushort RequestQueue;
        internal ushort IdleConnection;
        internal ushort HeaderWait;
        internal uint MinSendRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_BINDING_INFO
    {
        internal HTTP_FLAGS Flags;
        internal IntPtr RequestQueueHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_CONNECTION_LIMIT_INFO
    {
        internal HTTP_FLAGS Flags;
        internal uint MaxConnections;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTP_QOS_SETTING_INFO
    {
        internal HTTP_QOS_SETTING_TYPE QosType;
        internal IntPtr QosSetting;
    }

    // see http.w for definitions
    [Flags]
    internal enum HTTP_FLAGS : uint
    {
        NONE = 0x00000000,
        HTTP_RECEIVE_SECURE_CHANNEL_TOKEN = 0x00000001,
        HTTP_SEND_RESPONSE_FLAG_DISCONNECT = 0x00000001,
        HTTP_SEND_RESPONSE_FLAG_MORE_DATA = 0x00000002,
        HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA = 0x00000004,
        HTTP_SEND_RESPONSE_FLAG_RAW_HEADER = 0x00000004,
        HTTP_SEND_REQUEST_FLAG_MORE_DATA = 0x00000001,
        HTTP_PROPERTY_FLAG_PRESENT = 0x00000001,
        HTTP_INITIALIZE_CBT = 0x00000004,
        HTTP_SEND_RESPONSE_FLAG_OPAQUE = 0x00000040,
        HTTP_SEND_RESPONSE_FLAG_GOAWAY = 0x00000100,
    }

    [Flags]
    internal enum HTTP_AUTH_TYPES : uint
    {
        NONE = 0x00000000,
        HTTP_AUTH_ENABLE_BASIC = 0x00000001,
        HTTP_AUTH_ENABLE_DIGEST = 0x00000002,
        HTTP_AUTH_ENABLE_NTLM = 0x00000004,
        HTTP_AUTH_ENABLE_NEGOTIATE = 0x00000008,
        HTTP_AUTH_ENABLE_KERBEROS = 0x00000010,
    }

    [Flags]
    internal enum HTTP_CREATE_REQUEST_QUEUE_FLAG : uint
    {
        None = 0,
        // The HTTP_CREATE_REQUEST_QUEUE_FLAG_OPEN_EXISTING flag allows applications to open an existing request queue by name and retrieve the request queue handle. The pName parameter must contain a valid request queue name; it cannot be NULL.
        OpenExisting = 1,
        // The handle to the request queue created using this flag cannot be used to perform I/O operations. This flag can be set only when the request queue handle is created.
        Controller = 2,
        Delegation = 8
    }

    internal static class HTTP_RESPONSE_HEADER_ID
    {
        private static readonly string[] _strings =
        {
                    "Cache-Control",
                    "Connection",
                    "Date",
                    "Keep-Alive",
                    "Pragma",
                    "Trailer",
                    "Transfer-Encoding",
                    "Upgrade",
                    "Via",
                    "Warning",

                    "Allow",
                    "Content-Length",
                    "Content-Type",
                    "Content-Encoding",
                    "Content-Language",
                    "Content-Location",
                    "Content-MD5",
                    "Content-Range",
                    "Expires",
                    "Last-Modified",

                    "Accept-Ranges",
                    "Age",
                    "ETag",
                    "Location",
                    "Proxy-Authenticate",
                    "Retry-After",
                    "Server",
                    "Set-Cookie",
                    "Vary",
                    "WWW-Authenticate",
                };

        private static readonly Dictionary<string, int> _lookupTable = CreateLookupTable();

        private static Dictionary<string, int> CreateLookupTable()
        {
            Dictionary<string, int> lookupTable = new Dictionary<string, int>((int)Enum.HttpHeaderResponseMaximum, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < (int)Enum.HttpHeaderResponseMaximum; i++)
            {
                lookupTable.Add(_strings[i], i);
            }
            return lookupTable;
        }

        internal static int IndexOfKnownHeader(string HeaderName)
        {
            int index;
            return _lookupTable.TryGetValue(HeaderName, out index) ? index : -1;
        }

        internal enum Enum
        {
            HttpHeaderCacheControl = 0,    // general-header [section 4.5]
            HttpHeaderConnection = 1,    // general-header [section 4.5]
            HttpHeaderDate = 2,    // general-header [section 4.5]
            HttpHeaderKeepAlive = 3,    // general-header [not in rfc]
            HttpHeaderPragma = 4,    // general-header [section 4.5]
            HttpHeaderTrailer = 5,    // general-header [section 4.5]
            HttpHeaderTransferEncoding = 6,    // general-header [section 4.5]
            HttpHeaderUpgrade = 7,    // general-header [section 4.5]
            HttpHeaderVia = 8,    // general-header [section 4.5]
            HttpHeaderWarning = 9,    // general-header [section 4.5]

            HttpHeaderAllow = 10,   // entity-header  [section 7.1]
            HttpHeaderContentLength = 11,   // entity-header  [section 7.1]
            HttpHeaderContentType = 12,   // entity-header  [section 7.1]
            HttpHeaderContentEncoding = 13,   // entity-header  [section 7.1]
            HttpHeaderContentLanguage = 14,   // entity-header  [section 7.1]
            HttpHeaderContentLocation = 15,   // entity-header  [section 7.1]
            HttpHeaderContentMd5 = 16,   // entity-header  [section 7.1]
            HttpHeaderContentRange = 17,   // entity-header  [section 7.1]
            HttpHeaderExpires = 18,   // entity-header  [section 7.1]
            HttpHeaderLastModified = 19,   // entity-header  [section 7.1]

            // Response Headers

            HttpHeaderAcceptRanges = 20,   // response-header [section 6.2]
            HttpHeaderAge = 21,   // response-header [section 6.2]
            HttpHeaderEtag = 22,   // response-header [section 6.2]
            HttpHeaderLocation = 23,   // response-header [section 6.2]
            HttpHeaderProxyAuthenticate = 24,   // response-header [section 6.2]
            HttpHeaderRetryAfter = 25,   // response-header [section 6.2]
            HttpHeaderServer = 26,   // response-header [section 6.2]
            HttpHeaderSetCookie = 27,   // response-header [not in rfc]
            HttpHeaderVary = 28,   // response-header [section 6.2]
            HttpHeaderWwwAuthenticate = 29,   // response-header [section 6.2]

            HttpHeaderResponseMaximum = 30,

            HttpHeaderMaximum = 41
        }
    }
}
