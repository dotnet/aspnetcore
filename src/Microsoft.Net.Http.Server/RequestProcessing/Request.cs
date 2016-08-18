// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

//------------------------------------------------------------------------------
// <copyright file="HttpListenerRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.Http.Server
{
    public sealed class Request
    {
        private NativeRequestContext _nativeRequestContext;

        private X509Certificate2 _clientCert;
        private byte[] _providedTokenBindingId;
        private byte[] _referredTokenBindingId;

        private BoundaryType _contentBoundaryType;
        private long? _contentLength;
        private Stream _nativeStream;

        private SocketAddress _localEndPoint;
        private SocketAddress _remoteEndPoint;

        private bool _isDisposed = false;

        internal unsafe Request(RequestContext requestContext, NativeRequestContext memoryBlob)
        {
            // TODO: Verbose log
            RequestContext = requestContext;
            _nativeRequestContext = memoryBlob;
            _contentBoundaryType = BoundaryType.None;

            // Set up some of these now to avoid refcounting on memory blob later.
            RequestId = memoryBlob.RequestBlob->RequestId;
            UConnectionId = memoryBlob.RequestBlob->ConnectionId;
            SslStatus = memoryBlob.RequestBlob->pSslInfo == null ? SslStatus.Insecure :
                memoryBlob.RequestBlob->pSslInfo->SslClientCertNegotiated == 0 ? SslStatus.NoClientCert :
                SslStatus.ClientCert;
            if (memoryBlob.RequestBlob->pRawUrl != null && memoryBlob.RequestBlob->RawUrlLength > 0)
            {
                RawUrl = Marshal.PtrToStringAnsi((IntPtr)memoryBlob.RequestBlob->pRawUrl, memoryBlob.RequestBlob->RawUrlLength);
            }

            HttpApi.HTTP_COOKED_URL cookedUrl = memoryBlob.RequestBlob->CookedUrl;
            if (cookedUrl.pHost != null && cookedUrl.HostLength > 0)
            {
                // TODO: Unused
                // _cookedUrlHost = Marshal.PtrToStringUni((IntPtr)cookedUrl.pHost, cookedUrl.HostLength / 2);
            }
            var cookedUrlPath = string.Empty;
            if (cookedUrl.pAbsPath != null && cookedUrl.AbsPathLength > 0)
            {
                cookedUrlPath = Marshal.PtrToStringUni((IntPtr)cookedUrl.pAbsPath, cookedUrl.AbsPathLength / 2);
            }
            QueryString = string.Empty;
            if (cookedUrl.pQueryString != null && cookedUrl.QueryStringLength > 0)
            {
                QueryString = Marshal.PtrToStringUni((IntPtr)cookedUrl.pQueryString, cookedUrl.QueryStringLength / 2);
            }

            var prefix = requestContext.Server.Settings.UrlPrefixes.GetPrefix((int)memoryBlob.RequestBlob->UrlContext);
            var originalPath = RequestUriBuilder.GetRequestPath(RawUrl, cookedUrlPath, RequestContext.Logger);

            // These paths are both unescaped already.
            if (originalPath.Length == prefix.Path.Length - 1)
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

            int major = memoryBlob.RequestBlob->Version.MajorVersion;
            int minor = memoryBlob.RequestBlob->Version.MinorVersion;
            if (major == 1 && minor == 1)
            {
                ProtocolVersion = Constants.V1_1;
            }
            else if (major == 1 && minor == 0)
            {
                ProtocolVersion = Constants.V1_0;
            }
            else
            {
                ProtocolVersion = new Version(major, minor);
            }

            KnownMethod = memoryBlob.RequestBlob->Verb;
            Method = HttpApi.GetVerb(memoryBlob.RequestBlob);
            Headers = new HeaderCollection(new RequestHeaders(_nativeRequestContext));

            var requestV2 = (HttpApi.HTTP_REQUEST_V2*)memoryBlob.RequestBlob;
            User = AuthenticationManager.GetUser(requestV2->pRequestInfo, requestV2->RequestInfoCount);

            GetTlsTokenBindingInfo();

            // Finished directly accessing the HTTP_REQUEST structure.
            _nativeRequestContext.ReleasePins();
            // TODO: Verbose log parameters
        }

        internal byte[] RequestBuffer
        {
            get
            {
                CheckDisposed();
                return _nativeRequestContext.RequestBuffer;
            }
        }

        internal int BufferAlignment
        {
            get
            {
                CheckDisposed();
                return _nativeRequestContext.BufferAlignment;
            }
        }

        internal IntPtr OriginalBlobAddress
        {
            get
            {
                CheckDisposed();
                return _nativeRequestContext.OriginalBlobAddress;
            }
        }

        internal ulong UConnectionId { get; }

        // No ulongs in public APIs...
        public long ConnectionId => (long)UConnectionId;

        internal ulong RequestId { get; }

        private SslStatus SslStatus { get; }

        private RequestContext RequestContext { get; }

        // With the leading ?, if any
        public string QueryString { get; }

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

        public HeaderCollection Headers { get; }

        internal HttpApi.HTTP_VERB KnownMethod { get; }

        internal bool IsHeadMethod => KnownMethod == HttpApi.HTTP_VERB.HttpVerbHEAD;

        public string Method { get; }

        public Stream Body
        {
            get
            {
                if (_nativeStream == null)
                {
                    _nativeStream = HasEntityBody ? new RequestStream(RequestContext) : Stream.Null;
                }
                return _nativeStream;
            }
        }

        public string PathBase { get; }

        public string Path { get; }

        public bool IsHttps => SslStatus != SslStatus.Insecure;
        
        public string RawUrl { get; }

        public Version ProtocolVersion { get; }

        public bool HasEntityBody
        {
            get
            {
                // accessing the ContentLength property delay creates _contentBoundaryType
                return (ContentLength.HasValue && ContentLength.Value > 0 && _contentBoundaryType == BoundaryType.ContentLength)
                    || _contentBoundaryType == BoundaryType.Chunked;
            }
        }

        private SocketAddress RemoteEndPoint
        {
            get
            {
                if (_remoteEndPoint == null)
                {
                    _remoteEndPoint = HttpApi.GetRemoteEndPoint(RequestBuffer, BufferAlignment, OriginalBlobAddress);
                }

                return _remoteEndPoint;
            }
        }

        private SocketAddress LocalEndPoint
        {
            get
            {
                if (_localEndPoint == null)
                {
                    _localEndPoint = HttpApi.GetLocalEndPoint(RequestBuffer, BufferAlignment, OriginalBlobAddress);
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
        // TODO: >= Win8 check https://github.com/aspnet/WebListener/issues/215
        internal bool IsUpgradable => !HasEntityBody;

        public string ContentType => Headers[HttpKnownHeaderNames.ContentType];

        internal ClaimsPrincipal User { get; }

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

        public byte[] GetProvidedTokenBindingId()
        {
            return _providedTokenBindingId;
        }

        public byte[] GetReferredTokenBindingId()
        {
            return _referredTokenBindingId;
        }

        // Only call from the constructor so we can directly access the native request blob.
        // This requires Windows 10 and the following reg key:
        // Set Key: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\HTTP\Parameters to Value: EnableSslTokenBinding = 1 [DWORD]
        // Then for IE to work you need to set these:
        // Key: HKLM\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_TOKEN_BINDING
        // Value: "iexplore.exe"=dword:0x00000001
        // Key: HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_TOKEN_BINDING
        // Value: "iexplore.exe"=dword:00000001
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

        // should only be called from RequestContext
        internal void Dispose()
        {
            // TODO: Verbose log
            _isDisposed = true;
            _nativeRequestContext.Dispose();
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
            if (_nativeStream == null || _nativeStream == Stream.Null)
            {
                _nativeStream = new RequestStream(RequestContext);
            }
        }
    }
}
