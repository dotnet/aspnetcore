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
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Microsoft.AspNet.Server.WebListener
{
    internal sealed unsafe class Request : IDisposable
    {
        private RequestContext _requestContext;
        private NativeRequestContext _nativeRequestContext;

        private ulong _requestId;
        private ulong _connectionId;
        private ulong _contextId;

        private SslStatus _sslStatus;

        private string _httpMethod;
        private Version _httpVersion;

        private Uri _requestUri;
        private string _rawUrl;
        private string _cookedUrlHost;
        private string _cookedUrlPath;
        private string _cookedUrlQuery;

        private RequestHeaders _headers;
        private BoundaryType _contentBoundaryType;
        private long _contentLength;
        private Stream _requestStream;
        private SocketAddress _localEndPoint;
        private SocketAddress _remoteEndPoint;

        private IPrincipal _user;

        private bool _isDisposed = false;
        private CancellationTokenRegistration _disconnectRegistration;
        
        internal Request(RequestContext httpContext, NativeRequestContext memoryBlob)
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

        // Without the leading ?
        internal string Query
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_cookedUrlQuery))
                {
                    return _cookedUrlQuery.Substring(1);
                }
                return string.Empty;
            }
        }

        internal ulong RequestId
        {
            get
            {
                return _requestId;
            }
        }

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

        internal IDictionary<string, string[]> Headers
        {
            get
            {
                return _headers;
            }
        }

        internal string HttpMethod
        {
            get
            {
                return _httpMethod;
            }
        }

        internal Stream InputStream
        {
            get
            {
                if (_requestStream == null)
                {
                    // TODO: Move this to the constructor (or a lazy Env dictionary)
                    _requestStream = HasEntityBody ? new RequestStream(RequestContext) : Stream.Null;
                }
                return _requestStream;
            }
        }

        internal bool IsLocal
        {
            get
            {
                return LocalEndPoint.GetIPAddressString().Equals(RemoteEndPoint.GetIPAddressString());
            }
        }

        internal bool IsSecureConnection
        {
            get
            {
                return _sslStatus != SslStatus.Insecure;
            }
        }

        internal string RawUrl
        {
            get
            {
                return _rawUrl;
            }
        }

        internal Version ProtocolVersion
        {
            get
            {
                return _httpVersion;
            }
        }

        internal string Protocol
        {
            get
            {
                if (_httpVersion.Major == 1)
                {
                    if (_httpVersion.Minor == 1)
                    {
                        return "HTTP/1.1";
                    }
                    else if (_httpVersion.Minor == 0)
                    {
                        return "HTTP/1.0";
                    }
                }
                return "HTTP/" + _httpVersion.ToString(2);
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

        internal string RequestScheme
        {
            get
            {
                return IsSecureConnection ? Constants.HttpsScheme : Constants.HttpScheme;
            }
        }

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
            if (_requestStream != null)
            {
                _requestStream.Dispose();
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
            if (_requestStream == null || _requestStream == Stream.Null)
            {
                _requestStream = new RequestStream(RequestContext);
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
