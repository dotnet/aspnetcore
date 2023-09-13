
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Net.Http.Headers;
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTPAPI_VERSION
    {
        internal ushort HttpApiMajorVersion;
        internal ushort HttpApiMinorVersion;

        public static explicit operator HTTPAPI_VERSION(Windows.Win32.Networking.HttpServer.HTTPAPI_VERSION v) => new()
        {
            HttpApiMajorVersion = v.HttpApiMajorVersion,
            HttpApiMinorVersion = v.HttpApiMinorVersion
        };
    }

    internal enum HTTP_RESPONSE_INFO_FLAGS : uint
    {
        None = 0,
        PreserveOrder = 1,
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

    internal static class ResponseHeaders
    {
        private static readonly string[] _strings =
        {
            HeaderNames.CacheControl,
            HeaderNames.Connection,
            HeaderNames.Date,
            HeaderNames.KeepAlive,
            HeaderNames.Pragma,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Upgrade,
            HeaderNames.Via,
            HeaderNames.Warning,

            HeaderNames.Allow,
            HeaderNames.ContentLength,
            HeaderNames.ContentType,
            HeaderNames.ContentEncoding,
            HeaderNames.ContentLanguage,
            HeaderNames.ContentLocation,
            HeaderNames.ContentMD5,
            HeaderNames.ContentRange,
            HeaderNames.Expires,
            HeaderNames.LastModified,

            HeaderNames.AcceptRanges,
            HeaderNames.Age,
            HeaderNames.ETag,
            HeaderNames.Location,
            HeaderNames.ProxyAuthenticate,
            HeaderNames.RetryAfter,
            HeaderNames.Server,
            HeaderNames.SetCookie,
            HeaderNames.Vary,
            HeaderNames.WWWAuthenticate,
        };

        internal static FrozenDictionary<string, int> KnownHeaders { get; } = CreateLookupTable();

        private static FrozenDictionary<string, int> CreateLookupTable()
        {
            var lookupTable = new Dictionary<string, int>((int)HTTP_HEADER_ID.HttpHeaderResponseMaximum, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < (int)HTTP_HEADER_ID.HttpHeaderResponseMaximum; i++)
            {
                lookupTable.Add(_strings[i], i);
            }
            return lookupTable.ToFrozenDictionary();
        }
    }
}
