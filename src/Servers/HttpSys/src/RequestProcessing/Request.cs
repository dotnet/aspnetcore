// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal sealed class Request
    {
        private NativeRequestContext _nativeRequestContext;

        private X509Certificate2 _clientCert;
        // TODO: https://github.com/aspnet/HttpSysServer/issues/231
        // private byte[] _providedTokenBindingId;
        // private byte[] _referredTokenBindingId;

        private BoundaryType _contentBoundaryType;

        private long? _contentLength;
        private RequestStream _nativeStream;

        private AspNetCore.HttpSys.Internal.SocketAddress _localEndPoint;
        private AspNetCore.HttpSys.Internal.SocketAddress _remoteEndPoint;

        private bool _isDisposed = false;

        internal Request(RequestContext requestContext)
        {
            // TODO: Verbose log
            RequestContext = requestContext;
            Headers = new RequestHeaders();
        }

        internal void Initialize(NativeRequestContext nativeRequestContext)
        {
            Headers.Initialize(nativeRequestContext);

            _nativeRequestContext = nativeRequestContext;
            _contentBoundaryType = BoundaryType.None;

            RequestId = nativeRequestContext.RequestId;
            UConnectionId = nativeRequestContext.ConnectionId;
            SslStatus = nativeRequestContext.SslStatus;

            KnownMethod = nativeRequestContext.VerbId;
            Method = _nativeRequestContext.GetVerb();

            RawUrl = nativeRequestContext.GetRawUrl();

            var cookedUrl = nativeRequestContext.GetCookedUrl();
            QueryString = cookedUrl.GetQueryString() ?? string.Empty;

            var prefix = RequestContext.Server.Options.UrlPrefixes.GetPrefix((int)nativeRequestContext.UrlContext);

            var rawUrlInBytes = _nativeRequestContext.GetRawUrlInBytes();
            var originalPath = RequestUriBuilder.DecodeAndUnescapePath(rawUrlInBytes);

            // 'OPTIONS * HTTP/1.1'
            if (KnownMethod == HttpApiTypes.HTTP_VERB.HttpVerbOPTIONS && string.Equals(RawUrl, "*", StringComparison.Ordinal))
            {
                PathBase = string.Empty;
                Path = string.Empty;
            }
            // These paths are both unescaped already.
            else if (originalPath.Length == prefix.Path.Length - 1)
            {
                // They matched exactly except for the trailing slash.
                PathBase = originalPath;
                Path = string.Empty;
            }
            else
            {
                // url: /base/path, prefix: /base/, base: /base, path: /path
                // url: /, prefix: /, base: , path: /
                PathBase = originalPath.Substring(0, prefix.Path.Length - 1);
                Path = originalPath.Substring(prefix.Path.Length - 1);
            }

            ProtocolVersion = _nativeRequestContext.GetVersion();


            User = _nativeRequestContext.GetUser();

            if (IsHttps)
            {
                GetTlsHandshakeResults();
            }

            // GetTlsTokenBindingInfo(); TODO: https://github.com/aspnet/HttpSysServer/issues/231

            // Finished directly accessing the HTTP_REQUEST structure.
            _nativeRequestContext.ReleasePins();
            // TODO: Verbose log parameters
        }

        internal void Reset()
        {
            // Reset fields in sequential order according to layout for better memory/cache access behaviour
            /*
            Type layout for 'Request'
            Size: 184 bytes. Paddings: 2 bytes (%1 of empty space)
            |================================================================================|
            | Object Header (8 bytes)                                                        |
            |--------------------------------------------------------------------------------|
            | Method Table Ptr (8 bytes)                                                     |
            |================================================================================|
            |   0-7: NativeRequestContext _nativeRequestContext (8 bytes)                    |
            |--------------------------------------------------------------------------------|
            |  8-15: X509Certificate2 _clientCert (8 bytes)                                  |
            |--------------------------------------------------------------------------------|
            | 16-23: RequestStream _nativeStream (8 bytes)                                   |
            |--------------------------------------------------------------------------------|
            | 24-31: SocketAddress _localEndPoint (8 bytes)                                  |
            |--------------------------------------------------------------------------------|
            | 32-39: SocketAddress _remoteEndPoint (8 bytes)                                 |
            |--------------------------------------------------------------------------------|
            | 40-47: RequestContext <RequestContext>k__BackingField (8 bytes)                |
            |--------------------------------------------------------------------------------|
            | 48-55: String <QueryString>k__BackingField (8 bytes)                           |
            |--------------------------------------------------------------------------------|
            | 56-63: RequestHeaders <Headers>k__BackingField (8 bytes)                       |
            |--------------------------------------------------------------------------------|
            | 64-71: String <Method>k__BackingField (8 bytes)                                |
            |--------------------------------------------------------------------------------|
            | 72-79: String <PathBase>k__BackingField (8 bytes)                              |
            |--------------------------------------------------------------------------------|
            | 80-87: String <Path>k__BackingField (8 bytes)                                  |
            |--------------------------------------------------------------------------------|
            | 88-95: String <RawUrl>k__BackingField (8 bytes)                                |
            |--------------------------------------------------------------------------------|
            | 96-103: Version <ProtocolVersion>k__BackingField (8 bytes)                     |
            |--------------------------------------------------------------------------------|
            | 104-111: WindowsPrincipal <User>k__BackingField (8 bytes)                      |
            |--------------------------------------------------------------------------------|
            | 112-119: UInt64 <UConnectionId>k__BackingField (8 bytes)                       |
            |--------------------------------------------------------------------------------|
            | 120-127: UInt64 <RequestId>k__BackingField (8 bytes)                           |
            |--------------------------------------------------------------------------------|
            | 128-131: BoundaryType _contentBoundaryType (4 bytes)                           |
            |--------------------------------------------------------------------------------|
            | 132-135: HTTP_VERB <KnownMethod>k__BackingField (4 bytes)                      |
            |--------------------------------------------------------------------------------|
            | 136-139: SslProtocols <Protocol>k__BackingField (4 bytes)                      |
            |--------------------------------------------------------------------------------|
            | 140-143: CipherAlgorithmType <CipherAlgorithm>k__BackingField (4 bytes)        |
            |--------------------------------------------------------------------------------|
            | 144-147: Int32 <CipherStrength>k__BackingField (4 bytes)                       |
            |--------------------------------------------------------------------------------|
            | 148-151: HashAlgorithmType <HashAlgorithm>k__BackingField (4 bytes)            |
            |--------------------------------------------------------------------------------|
            | 152-155: Int32 <HashStrength>k__BackingField (4 bytes)                         |
            |--------------------------------------------------------------------------------|
            | 156-159: ExchangeAlgorithmType <KeyExchangeAlgorithm>k__BackingField (4 bytes) |
            |--------------------------------------------------------------------------------|
            | 160-163: Int32 <KeyExchangeStrength>k__BackingField (4 bytes)                  |
            |--------------------------------------------------------------------------------|
            |   164: Boolean _isDisposed (1 byte)                                            |
            |--------------------------------------------------------------------------------|
            |   165: SslStatus <SslStatus>k__BackingField (1 byte)                           |
            |--------------------------------------------------------------------------------|
            | 166-167: padding (2 bytes)                                                     |
            |--------------------------------------------------------------------------------|
            | 168-183: Nullable`1 _contentLength (16 bytes)                                  |
            |================================================================================|
             */
            _nativeRequestContext = null;
            _clientCert = null;
            _nativeStream = null;
            _localEndPoint = null;
            _remoteEndPoint = null;
            // RequestContext - kept
            QueryString = null;
            Headers.Reset();
            Method = null;
            PathBase = null;
            Path = null;
            RawUrl = null;
            ProtocolVersion = null;
            User = null;
            UConnectionId = 0;
            RequestId = 0;
            _contentBoundaryType = BoundaryType.None;
            KnownMethod = HttpApiTypes.HTTP_VERB.HttpVerbUnknown;
            Protocol = SslProtocols.None;
            CipherAlgorithm = CipherAlgorithmType.None;
            CipherStrength = 0;
            HashAlgorithm = HashAlgorithmType.None;
            HashStrength = 0;
            KeyExchangeAlgorithm = ExchangeAlgorithmType.None;
            KeyExchangeStrength = 0;
            _isDisposed = false;
            SslStatus = SslStatus.Insecure;
            _contentLength = null;
        }

        internal ulong UConnectionId { get; private set; }

        // No ulongs in public APIs...
        public long ConnectionId => (long)UConnectionId;

        internal ulong RequestId { get; private set; }

        private SslStatus SslStatus { get; set; }

        private RequestContext RequestContext { get; set; }

        // With the leading ?, if any
        public string QueryString { get; private set; }

        public long? ContentLength
        {
            get
            {
                if (_contentBoundaryType == BoundaryType.None)
                {
                    string transferEncoding = Headers[HttpKnownHeaderNames.TransferEncoding];
                    if (string.Equals("chunked", transferEncoding?.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        _contentBoundaryType = BoundaryType.Chunked;
                    }
                    else
                    {
                        string length = Headers[HttpKnownHeaderNames.ContentLength];
                        long value;
                        if (length != null && long.TryParse(length.Trim(), NumberStyles.None,
                            CultureInfo.InvariantCulture.NumberFormat, out value))
                        {
                            _contentBoundaryType = BoundaryType.ContentLength;
                            _contentLength = value;
                        }
                        else
                        {
                            _contentBoundaryType = BoundaryType.Invalid;
                        }
                    }
                }

                return _contentLength;
            }
        }

        internal RequestHeaders Headers { get; }

        internal HttpApiTypes.HTTP_VERB KnownMethod { get; private set; }

        internal bool IsHeadMethod => KnownMethod == HttpApiTypes.HTTP_VERB.HttpVerbHEAD;

        public string Method { get; private set; }

        public Stream Body => EnsureRequestStream() ?? Stream.Null;

        private RequestStream EnsureRequestStream()
        {
            if (_nativeStream == null && HasEntityBody)
            {
                _nativeStream = new RequestStream(RequestContext);
            }
            return _nativeStream;
        }

        public bool HasRequestBodyStarted => _nativeStream?.HasStarted ?? false;

        public long? MaxRequestBodySize
        {
            get => EnsureRequestStream()?.MaxSize;
            set
            {
                EnsureRequestStream();
                if (_nativeStream != null)
                {
                    _nativeStream.MaxSize = value;
                }
            }
        }

        public string PathBase { get; private set; }

        public string Path { get; private set; }

        public bool IsHttps => SslStatus != SslStatus.Insecure;

        public string RawUrl { get; private set; }

        public Version ProtocolVersion { get; private set; }

        public bool HasEntityBody
        {
            get
            {
                // accessing the ContentLength property delay creates _contentBoundaryType
                return (ContentLength.HasValue && ContentLength.Value > 0 && _contentBoundaryType == BoundaryType.ContentLength)
                    || _contentBoundaryType == BoundaryType.Chunked;
            }
        }

        private AspNetCore.HttpSys.Internal.SocketAddress RemoteEndPoint
        {
            get
            {
                if (_remoteEndPoint == null)
                {
                    _remoteEndPoint = _nativeRequestContext.GetRemoteEndPoint();
                }

                return _remoteEndPoint;
            }
        }

        private AspNetCore.HttpSys.Internal.SocketAddress LocalEndPoint
        {
            get
            {
                if (_localEndPoint == null)
                {
                    _localEndPoint = _nativeRequestContext.GetLocalEndPoint();
                }

                return _localEndPoint;
            }
        }

        // TODO: Lazy cache?
        public IPAddress RemoteIpAddress => RemoteEndPoint.GetIPAddress();

        public IPAddress LocalIpAddress => LocalEndPoint.GetIPAddress();

        public int RemotePort => RemoteEndPoint.GetPort();

        public int LocalPort => LocalEndPoint.GetPort();

        public string Scheme => IsHttps ? Constants.HttpsScheme : Constants.HttpScheme;

        // HTTP.Sys allows you to upgrade anything to opaque unless content-length > 0 or chunked are specified.
        internal bool IsUpgradable => !HasEntityBody && ComNetOS.IsWin8orLater;

        internal WindowsPrincipal User { get; private set; }

        public SslProtocols Protocol { get; private set; }

        public CipherAlgorithmType CipherAlgorithm { get; private set; }

        public int CipherStrength { get; private set; }

        public HashAlgorithmType HashAlgorithm { get; private set; }

        public int HashStrength { get; private set; }

        public ExchangeAlgorithmType KeyExchangeAlgorithm { get; private set; }

        public int KeyExchangeStrength { get; private set; }

        private void GetTlsHandshakeResults()
        {
            var handshake = _nativeRequestContext.GetTlsHandshake();

            Protocol = handshake.Protocol;
            // The OS considers client and server TLS as different enum values. SslProtocols choose to combine those for some reason.
            // We need to fill in the client bits so the enum shows the expected protocol.
            // https://docs.microsoft.com/en-us/windows/desktop/api/schannel/ns-schannel-_secpkgcontext_connectioninfo
            // Compare to https://referencesource.microsoft.com/#System/net/System/Net/SecureProtocols/_SslState.cs,8905d1bf17729de3
#pragma warning disable CS0618 // Type or member is obsolete
            if ((Protocol & SslProtocols.Ssl2) != 0)
            {
                Protocol |= SslProtocols.Ssl2;
            }
            if ((Protocol & SslProtocols.Ssl3) != 0)
            {
                Protocol |= SslProtocols.Ssl3;
            }
#pragma warning restore CS0618 // Type or member is obsolete
            if ((Protocol & SslProtocols.Tls) != 0)
            {
                Protocol |= SslProtocols.Tls;
            }
            if ((Protocol & SslProtocols.Tls11) != 0)
            {
                Protocol |= SslProtocols.Tls11;
            }
            if ((Protocol & SslProtocols.Tls12) != 0)
            {
                Protocol |= SslProtocols.Tls12;
            }

            CipherAlgorithm = handshake.CipherType;
            CipherStrength = (int)handshake.CipherStrength;
            HashAlgorithm = handshake.HashType;
            HashStrength = (int)handshake.HashStrength;
            KeyExchangeAlgorithm = handshake.KeyExchangeType;
            KeyExchangeStrength = (int)handshake.KeyExchangeStrength;
        }

        // Populates the client certificate.  The result may be null if there is no client cert.
        // TODO: Does it make sense for this to be invoked multiple times (e.g. renegotiate)? Client and server code appear to
        // enable this, but it's unclear what Http.Sys would do.
        public async Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (SslStatus == SslStatus.Insecure)
            {
                // Non-SSL
                return null;
            }
            // TODO: Verbose log
            if (_clientCert != null)
            {
                return _clientCert;
            }
            cancellationToken.ThrowIfCancellationRequested();

            var certLoader = new ClientCertLoader(RequestContext, cancellationToken);
            try
            {
                await certLoader.LoadClientCertificateAsync().SupressContext();
                // Populate the environment.
                if (certLoader.ClientCert != null)
                {
                    _clientCert = certLoader.ClientCert;
                }
                // TODO: Expose errors and exceptions?
            }
            catch (Exception)
            {
                if (certLoader != null)
                {
                    certLoader.Dispose();
                }
                throw;
            }
            return _clientCert;
        }
        /* TODO: https://github.com/aspnet/WebListener/issues/231
        private byte[] GetProvidedTokenBindingId()
        {
            return _providedTokenBindingId;
        }

        private byte[] GetReferredTokenBindingId()
        {
            return _referredTokenBindingId;
        }
        */
        // Only call from the constructor so we can directly access the native request blob.
        // This requires Windows 10 and the following reg key:
        // Set Key: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\HTTP\Parameters to Value: EnableSslTokenBinding = 1 [DWORD]
        // Then for IE to work you need to set these:
        // Key: HKLM\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_TOKEN_BINDING
        // Value: "iexplore.exe"=dword:0x00000001
        // Key: HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_TOKEN_BINDING
        // Value: "iexplore.exe"=dword:00000001
        // TODO: https://github.com/aspnet/WebListener/issues/231
        // TODO: https://github.com/aspnet/WebListener/issues/204 Move to NativeRequestContext
        /*
        private unsafe void GetTlsTokenBindingInfo()
        {
            var nativeRequest = (HttpApi.HTTP_REQUEST_V2*)_nativeRequestContext.RequestBlob;
            for (int i = 0; i < nativeRequest->RequestInfoCount; i++)
            {
                var pThisInfo = &nativeRequest->pRequestInfo[i];
                if (pThisInfo->InfoType == HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeSslTokenBinding)
                {
                    var pTokenBindingInfo = (HttpApi.HTTP_REQUEST_TOKEN_BINDING_INFO*)pThisInfo->pInfo;
                    _providedTokenBindingId = TokenBindingUtil.GetProvidedTokenIdFromBindingInfo(pTokenBindingInfo, out _referredTokenBindingId);
                }
            }
        }
        */
        internal uint GetChunks(ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size)
        {
            return _nativeRequestContext.GetChunks(ref dataChunkIndex, ref dataChunkOffset, buffer, offset, size);
        }

        // should only be called from RequestContext
        internal void Dispose()
        {
            // TODO: Verbose log
            _isDisposed = true;
            _nativeRequestContext.Dispose();
            (User?.Identity as WindowsIdentity)?.Dispose();
            if (_nativeStream != null)
            {
                _nativeStream.Dispose();
            }
        }

        private void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        internal void SwitchToOpaqueMode()
        {
            if (_nativeStream == null)
            {
                _nativeStream = new RequestStream(RequestContext);
            }
            _nativeStream.SwitchToOpaqueMode();
        }
    }
}
