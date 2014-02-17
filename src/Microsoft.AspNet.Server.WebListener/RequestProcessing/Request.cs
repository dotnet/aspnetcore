//------------------------------------------------------------------------------
// <copyright file="HttpListenerRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
#if NET45
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
#endif
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Server.WebListener
{
    internal sealed class Request : IHttpRequestInformation, IHttpConnection, IHttpTransportLayerSecurity, IDisposable
    {
        private RequestContext _requestContext;
        private NativeRequestContext _nativeRequestContext;

        private ulong _requestId;
        private ulong _connectionId;
        private ulong _contextId;

        private SslStatus _sslStatus;
        private string _scheme;

        private string _httpMethod;
        private Version _httpVersion;
        private string _httpProtocolVersion;

        // private Uri _requestUri;
        private string _rawUrl;
        private string _cookedUrlHost;
        private string _cookedUrlPath;
        private string _cookedUrlQuery;
        private string _pathBase;
        private string _path;

#if NET45
        private X509Certificate _clientCert;
#endif

        private IDictionary<string, string[]> _headers;
        private BoundaryType _contentBoundaryType;
        private long _contentLength;
        private Stream _nativeStream;
        private Stream _requestStream;

        private SocketAddress _localEndPoint;
        private SocketAddress _remoteEndPoint;
#if NET45
        private IPAddress _remoteIpAddress;
        private IPAddress _localIpAddress;
#endif
        private int? _remotePort;
        private int? _localPort;
        private bool? _isLocal;

        private IPrincipal _user;

        private bool _isDisposed = false;
        private CancellationTokenRegistration _disconnectRegistration;
        
        internal unsafe Request(RequestContext httpContext, NativeRequestContext memoryBlob)
        {
            // TODO: Verbose log
            _requestContext = httpContext;
            _nativeRequestContext = memoryBlob;
            _contentBoundaryType = BoundaryType.None;

            // Set up some of these now to avoid refcounting on memory blob later.
            _requestId = memoryBlob.RequestBlob->RequestId;
            _connectionId = memoryBlob.RequestBlob->ConnectionId;
            _contextId = memoryBlob.RequestBlob->UrlContext;
            _sslStatus = memoryBlob.RequestBlob->pSslInfo == null ? SslStatus.Insecure :
                memoryBlob.RequestBlob->pSslInfo->SslClientCertNegotiated == 0 ? SslStatus.NoClientCert :
                SslStatus.ClientCert;
            if (memoryBlob.RequestBlob->pRawUrl != null && memoryBlob.RequestBlob->RawUrlLength > 0)
            {
                _rawUrl = Marshal.PtrToStringAnsi((IntPtr)memoryBlob.RequestBlob->pRawUrl, memoryBlob.RequestBlob->RawUrlLength);
            }

            UnsafeNclNativeMethods.HttpApi.HTTP_COOKED_URL cookedUrl = memoryBlob.RequestBlob->CookedUrl;
            if (cookedUrl.pHost != null && cookedUrl.HostLength > 0)
            {
                _cookedUrlHost = Marshal.PtrToStringUni((IntPtr)cookedUrl.pHost, cookedUrl.HostLength / 2);
            }
            if (cookedUrl.pAbsPath != null && cookedUrl.AbsPathLength > 0)
            {
                _cookedUrlPath = Marshal.PtrToStringUni((IntPtr)cookedUrl.pAbsPath, cookedUrl.AbsPathLength / 2);
            }
            if (cookedUrl.pQueryString != null && cookedUrl.QueryStringLength > 0)
            {
                _cookedUrlQuery = Marshal.PtrToStringUni((IntPtr)cookedUrl.pQueryString, cookedUrl.QueryStringLength / 2);
            }

            Prefix prefix = httpContext.Server.UriPrefixes[(int)_contextId];
            string orriginalPath = RequestPath;

            // These paths are both unescaped already.
            if (orriginalPath.Length == prefix.Path.Length - 1)
            {
                // They matched exactly except for the trailing slash.
                _pathBase = orriginalPath;
                _path = string.Empty;
            }
            else
            {
                // url: /base/path, prefix: /base/, base: /base, path: /path
                // url: /, prefix: /, base: , path: /
                _pathBase = orriginalPath.Substring(0, prefix.Path.Length - 1);
                _path = orriginalPath.Substring(prefix.Path.Length - 1);
            }

            int major = memoryBlob.RequestBlob->Version.MajorVersion;
            int minor = memoryBlob.RequestBlob->Version.MinorVersion;
            if (major == 1 && minor == 1)
            {
                _httpVersion = Constants.V1_1;
            }
            else if (major == 1 && minor == 0)
            {
                _httpVersion = Constants.V1_0;
            }
            else
            {
                _httpVersion = new Version(major, minor);
            }

            _httpMethod = UnsafeNclNativeMethods.HttpApi.GetVerb(RequestBuffer, OriginalBlobAddress);
            _headers = new RequestHeaders(_nativeRequestContext);

            UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_V2* requestV2 = (UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_V2*)memoryBlob.RequestBlob;
            _user = GetUser(requestV2->pRequestInfo);

            // TODO: Verbose log parameters

            // TODO: Verbose log headers
        }

        internal SslStatus SslStatus
        {
            get
            {
                return _sslStatus;
            }
        }

        internal ulong ConnectionId
        {
            get
            {
                return _connectionId;
            }
        }

        internal ulong ContextId
        {
            get { return _contextId; }
        }

        internal RequestContext RequestContext
        {
            get
            {
                return _requestContext;
            }
        }

        internal byte[] RequestBuffer
        {
            get
            {
                CheckDisposed();
                return _nativeRequestContext.RequestBuffer;
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

        // With the leading ?, if any
        public string QueryString
        {
            get
            {
                return _cookedUrlQuery ?? string.Empty;
            }
            set
            {
                _cookedUrlQuery = value;
            }
        }

        internal ulong RequestId
        {
            get
            {
                return _requestId;
            }
        }

#if NET45
        X509Certificate IHttpTransportLayerSecurity.ClientCertificate
        {
            get
            {
                if (_clientCert == null)
                {
                    // TODO: Sync
                    ((IHttpTransportLayerSecurity)this).LoadAsync().Wait();
                }
                return _clientCert;
            }
            set
            {
                _clientCert = value;
            }
        }
#endif

        // TODO: Move this to the constructor, that's where it will be called.
        internal long ContentLength64
        {
            get
            {
                if (_contentBoundaryType == BoundaryType.None)
                {
                    string transferEncoding = Headers.Get(HttpKnownHeaderNames.TransferEncoding) ?? string.Empty;
                    if ("chunked".Equals(transferEncoding.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        _contentBoundaryType = BoundaryType.Chunked;
                        _contentLength = -1;
                    }
                    else
                    {
                        _contentLength = 0;
                        _contentBoundaryType = BoundaryType.ContentLength;
                        string length = Headers.Get(HttpKnownHeaderNames.ContentLength) ?? string.Empty;
                        if (length != null)
                        {
                            if (!long.TryParse(length.Trim(), NumberStyles.None,
                                CultureInfo.InvariantCulture.NumberFormat, out _contentLength))
                            {
                                _contentLength = 0;
                                _contentBoundaryType = BoundaryType.Invalid;
                            }
                        }
                    }
                }

                return _contentLength;
            }
        }

        public IDictionary<string, string[]> Headers
        {
            get
            {
                return _headers;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _headers = value;
            }
        }

        public string Method
        {
            get
            {
                return _httpMethod;
            }
            set
            {
                _httpMethod = value;
            }
        }

        internal Stream NativeStream
        {
            get
            {
                if (_nativeStream == null)
                {
                    // TODO: Move this to the constructor (or a lazy Env dictionary)
                    _nativeStream = HasEntityBody ? new RequestStream(RequestContext) : Stream.Null;
                }
                return _nativeStream;
            }
        }

        public Stream Body
        {
            get
            {
                if (_requestStream == null)
                {
                    // TODO: Move this to the constructor (or a lazy Env dictionary)
                    _requestStream = NativeStream;
                }
                return _requestStream;
            }
            set
            {
                _requestStream = value;
            }
        }

        public string PathBase
        {
            get
            {
                return _pathBase;
            }
            set
            {
                _pathBase = value;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
            }
        }

        public bool IsLocal
        {
            get
            {
                if (!_isLocal.HasValue)
                {
                    _isLocal = LocalEndPoint.GetIPAddress().Equals(RemoteEndPoint.GetIPAddress());
                }
                return _isLocal.Value;
            }
            set
            {
                _isLocal = value;
            }
        }

        internal bool IsSecureConnection
        {
            get
            {
                return _sslStatus != SslStatus.Insecure;
            }
        }
        /*
        internal string RawUrl
        {
            get
            {
                return _rawUrl;
            }
        }
        */
        internal Version ProtocolVersion
        {
            get
            {
                return _httpVersion;
            }
        }

        public string Protocol
        {
            get
            {
                if (_httpProtocolVersion == null)
                {
                    if (_httpVersion.Major == 1)
                    {
                        if (_httpVersion.Minor == 1)
                        {
                            _httpProtocolVersion = "HTTP/1.1";
                        }
                        else if (_httpVersion.Minor == 0)
                        {
                            _httpProtocolVersion = "HTTP/1.0";
                        }
                    }
                    else
                    {
                        _httpProtocolVersion = "HTTP/" + _httpVersion.ToString(2);
                    }
                }
                return _httpProtocolVersion;
            }
            set
            {
                // TODO: Set _httpVersion?
                _httpProtocolVersion = value;
            }
        }

        // TODO: Move this to the constructor
        internal bool HasEntityBody
        {
            get
            {
                // accessing the ContentLength property delay creates m_BoundaryType
                return (ContentLength64 > 0 && _contentBoundaryType == BoundaryType.ContentLength) ||
                    _contentBoundaryType == BoundaryType.Chunked || _contentBoundaryType == BoundaryType.Multipart;
            }
        }

        internal SocketAddress RemoteEndPoint
        {
            get
            {
                if (_remoteEndPoint == null)
                {
                    _remoteEndPoint = UnsafeNclNativeMethods.HttpApi.GetRemoteEndPoint(RequestBuffer, OriginalBlobAddress);
                }

                return _remoteEndPoint;
            }
        }

        internal SocketAddress LocalEndPoint
        {
            get
            {
                if (_localEndPoint == null)
                {
                    _localEndPoint = UnsafeNclNativeMethods.HttpApi.GetLocalEndPoint(RequestBuffer, OriginalBlobAddress);
                }

                return _localEndPoint;
            }
        }

        public IPAddress RemoteIpAddress
        {
            get
            {
                if (_remoteIpAddress == null)
                {
                    _remoteIpAddress = RemoteEndPoint.GetIPAddress();
                }
                return _remoteIpAddress;
            }
            set
            {
                _remoteIpAddress = value;
            }
        }

        public IPAddress LocalIpAddress
        {
            get
            {
                if (_localIpAddress == null)
                {
                    _localIpAddress = LocalEndPoint.GetIPAddress();
                }
                return _localIpAddress;
            }
            set
            {
                _localIpAddress = value;
            }
        }

        public int RemotePort
        {
            get
            {
                if (!_remotePort.HasValue)
                {
                    _remotePort = RemoteEndPoint.GetPort();
                }
                return _remotePort.Value;
            }
            set
            {
                _remotePort = value;
            }
        }

        public int LocalPort
        {
            get
            {
                if (!_localPort.HasValue)
                {
                    _localPort = LocalEndPoint.GetPort();
                }
                return _localPort.Value;
            }
            set
            {
                _localPort = value;
            }
        }

        public string Scheme
        {
            get
            {
                if (_scheme == null)
                {
                    _scheme = IsSecureConnection ? Constants.HttpsScheme : Constants.HttpScheme;
                }
                return _scheme;
            }
            set
            {
                _scheme = value;
            }
        }
        /*
        internal Uri RequestUri
        {
            get
            {
                if (_requestUri == null)
                {
                    _requestUri = RequestUriBuilder.GetRequestUri(
                        _rawUrl, RequestScheme, _cookedUrlHost, _cookedUrlPath, _cookedUrlQuery);
                }

                return _requestUri;
            }
        }
        */
        internal string RequestPath
        {
            get
            {
                return RequestUriBuilder.GetRequestPath(_rawUrl, _cookedUrlPath);
            }
        }

        internal bool IsUpgradable
        {
            get
            {
                // HTTP.Sys allows you to upgrade anything to opaque unless content-length > 0 or chunked are specified.
                return !HasEntityBody;
            }
        }

        internal IPrincipal User
        {
            get { return _user; }
        }

        private unsafe IPrincipal GetUser(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO* requestInfo)
        {
            if (requestInfo == null
                || requestInfo->InfoType != UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth)
            {
                return null;
            }

            if (requestInfo->pInfo->AuthStatus != UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
            {
                return null;
            }

#if NET45
            return new WindowsPrincipal(new WindowsIdentity(requestInfo->pInfo->AccessToken));
#else
            return null;
#endif
        }

        // Populates the client certificate.  The result may be null if there is no client cert.
        // TODO: Does it make sense for this to be invoked multiple times (e.g. renegotiate)? Client and server code appear to
        // enable this, but it's unclear what Http.Sys would do.
        async Task IHttpTransportLayerSecurity.LoadAsync()
        {
            if (SslStatus == SslStatus.Insecure)
            {
                // Non-SSL
                return;
            }
            // TODO: Verbose log
#if NET45
            if (_clientCert != null)
            {
                return;
            }

            ClientCertLoader certLoader = new ClientCertLoader(RequestContext);
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
#else
            throw new NotImplementedException();
#endif
        }

        // Use this to save the blob from dispose if this object was never used (never given to a user) and is about to be
        // disposed.
        internal void DetachBlob(NativeRequestContext memoryBlob)
        {
            if (memoryBlob != null && (object)memoryBlob == (object)_nativeRequestContext)
            {
                _nativeRequestContext = null;
            }
        }

        // Finalizes ownership of the memory blob.  DetachBlob can't be called after this.
        internal void ReleasePins()
        {
            _nativeRequestContext.ReleasePins();
        }

        // should only be called from RequestContext
        public void Dispose()
        {
            // TODO: Verbose log
            _isDisposed = true;
            NativeRequestContext memoryBlob = _nativeRequestContext;
            if (memoryBlob != null)
            {
                memoryBlob.Dispose();
                _nativeRequestContext = null;
            }
            _disconnectRegistration.Dispose();
            if (_nativeStream != null)
            {
                _nativeStream.Dispose();
            }
        }

        internal void CheckDisposed()
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

        internal void RegisterForDisconnect(CancellationToken cancellationToken)
        {
            _disconnectRegistration = cancellationToken.Register(Cancel, this);
        }

        private static void Cancel(object obj)
        {
            Request request = (Request)obj;
            // Cancels owin.CallCanceled
            request.RequestContext.Abort();
        }
    }
}
