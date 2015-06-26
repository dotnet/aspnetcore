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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Net.Http.Headers;
using Microsoft.Net.Http.Server;
using Microsoft.Net.WebSockets;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class FeatureContext :
        IHttpRequestFeature,
        IHttpConnectionFeature,
        IHttpResponseFeature,
        IHttpSendFileFeature,
        ITlsConnectionFeature,
        ITlsTokenBindingFeature,
        IHttpBufferingFeature,
        IHttpRequestLifetimeFeature,
        IHttpWebSocketFeature,
        IHttpAuthenticationFeature,
        IHttpUpgradeFeature,
        IHttpRequestIdentifierFeature
    {
        private static Func<object,Task> OnStartDelegate = OnStart;

        private RequestContext _requestContext;
        private FeatureCollection _features;
        private bool _enableResponseCaching;

        private Stream _requestBody;
        private IDictionary<string, string[]> _requestHeaders;
        private string _scheme;
        private string _httpMethod;
        private string _httpProtocolVersion;
        private string _query;
        private string _pathBase;
        private string _path;
        private IPAddress _remoteIpAddress;
        private IPAddress _localIpAddress;
        private int? _remotePort;
        private int? _localPort;
        private bool? _isLocal;
        private string _requestId;
        private X509Certificate2 _clientCert;
        private ClaimsPrincipal _user;
        private IAuthenticationHandler _authHandler;
        private CancellationToken? _disconnectToken;
        private Stream _responseStream;
        private IDictionary<string, string[]> _responseHeaders;

        internal FeatureContext(RequestContext requestContext, bool enableResponseCaching)
        {
            _requestContext = requestContext;
            _features = new FeatureCollection();
            _authHandler = new AuthenticationHandler(requestContext);
            _enableResponseCaching = enableResponseCaching;
            requestContext.Response.OnResponseStarting(OnStartDelegate, this);
            PopulateFeatures();
        }

        internal IFeatureCollection Features
        {
            get { return _features; }
        }

        private Request Request
        {
            get { return _requestContext.Request; }
        }

        private Response Response
        {
            get { return _requestContext.Response; }
        }

        private void PopulateFeatures()
        {
            _features.Add(typeof(IHttpRequestFeature), this);
            _features.Add(typeof(IHttpConnectionFeature), this);
            _features.Add(typeof(IHttpResponseFeature), this);
            _features.Add(typeof(IHttpSendFileFeature), this);
            _features.Add(typeof(IHttpBufferingFeature), this);
            _features.Add(typeof(IHttpRequestLifetimeFeature), this);
            _features.Add(typeof(IHttpAuthenticationFeature), this);
            _features.Add(typeof(IHttpRequestIdentifierFeature), this);

            if (Request.IsSecureConnection)
            {
                _features.Add(typeof(ITlsConnectionFeature), this);
                _features.Add(typeof(ITlsTokenBindingFeature), this);
            }

            // Win8+
            if (WebSocketHelpers.AreWebSocketsSupported)
            {
                _features.Add(typeof(IHttpUpgradeFeature), this);
                _features.Add(typeof(IHttpWebSocketFeature), this);
            }
        }

        Stream IHttpRequestFeature.Body
        {
            get
            {
                if (_requestBody == null)
                {
                    _requestBody = Request.Body;
                }
                return _requestBody;
            }
            set { _requestBody = value; }
        }

        IDictionary<string, string[]> IHttpRequestFeature.Headers
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = Request.Headers;
                }
                return _requestHeaders;
            }
            set { _requestHeaders = value; }
        }

        string IHttpRequestFeature.Method
        {
            get
            {
                if (_httpMethod == null)
                {
                    _httpMethod = Request.Method;
                }
                return _httpMethod;
            }
            set { _httpMethod = value; }
        }

        string IHttpRequestFeature.Path
        {
            get
            {
                if (_path == null)
                {
                    _path = Request.Path;
                }
                return _path;
            }
            set { _path = value; }
        }

        string IHttpRequestFeature.PathBase
        {
            get
            {
                if (_pathBase == null)
                {
                    _pathBase = Request.PathBase;
                }
                return _pathBase;
            }
            set { _pathBase = value; }
        }

        string IHttpRequestFeature.Protocol
        {
            get
            {
                if (_httpProtocolVersion == null)
                {
                    if (Request.ProtocolVersion.Major == 1)
                    {
                        if (Request.ProtocolVersion.Minor == 1)
                        {
                            _httpProtocolVersion = "HTTP/1.1";
                        }
                        else if (Request.ProtocolVersion.Minor == 0)
                        {
                            _httpProtocolVersion = "HTTP/1.0";
                        }
                    }

                    _httpProtocolVersion = "HTTP/" + Request.ProtocolVersion.ToString(2);
                }
                return _httpProtocolVersion;
            }
            set { _httpProtocolVersion = value; }
        }

        string IHttpRequestFeature.QueryString
        {
            get
            {
                if (_query == null)
                {
                    _query = Request.QueryString;
                }
                return _query;
            }
            set { _query = value; }
        }

        string IHttpRequestFeature.Scheme
        {
            get
            {
                if (_scheme == null)
                {
                    _scheme = Request.Scheme;
                }
                return _scheme;
            }
            set { _scheme = value; }
        }

        bool IHttpConnectionFeature.IsLocal
        {
            get
            {
                if (_isLocal == null)
                {
                    _isLocal = Request.IsLocal;
                }
                return _isLocal.Value;
            }
            set { _isLocal = value; }
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get
            {
                if (_localIpAddress == null)
                {
                    _localIpAddress = Request.LocalIpAddress;
                }
                return _localIpAddress;
            }
            set { _localIpAddress = value; }
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get
            {
                if (_remoteIpAddress == null)
                {
                    _remoteIpAddress = Request.RemoteIpAddress;
                }
                return _remoteIpAddress;
            }
            set { _remoteIpAddress = value; }
        }

        int IHttpConnectionFeature.LocalPort
        {
            get
            {
                if (_localPort == null)
                {
                    _localPort = Request.LocalPort;
                }
                return _localPort.Value;
            }
            set { _localPort = value; }
        }

        int IHttpConnectionFeature.RemotePort
        {
            get
            {
                if (_remotePort == null)
                {
                    _remotePort = Request.RemotePort;
                }
                return _remotePort.Value;
            }
            set { _remotePort = value; }
        }

        X509Certificate2 ITlsConnectionFeature.ClientCertificate
        {
            get
            {
                if (_clientCert == null)
                {
                    _clientCert = Request.GetClientCertificateAsync().Result; // TODO: Sync;
                }
                return _clientCert;
            }
            set { _clientCert = value; }
        }

        async Task<X509Certificate2> ITlsConnectionFeature.GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            if (_clientCert == null)
            {
                _clientCert = await Request.GetClientCertificateAsync(cancellationToken);
            }
            return _clientCert;
        }

        byte[] ITlsTokenBindingFeature.GetProvidedTokenBindingId()
        {
            return Request.GetProvidedTokenBindingId();
        }

        byte[] ITlsTokenBindingFeature.GetReferredTokenBindingId()
        {
            return Request.GetReferredTokenBindingId();
        }

        void IHttpBufferingFeature.DisableRequestBuffering()
        {
            // There is no request buffering.
        }

        void IHttpBufferingFeature.DisableResponseBuffering()
        {
            Response.ShouldBuffer = false;
        }

        Stream IHttpResponseFeature.Body
        {
            get
            {
                if (_responseStream == null)
                {
                    _responseStream = Response.Body;
                }
                return _responseStream;
            }
            set { _responseStream = value; }
        }

        IDictionary<string, string[]> IHttpResponseFeature.Headers
        {
            get
            {
                if (_responseHeaders == null)
                {
                    _responseHeaders = Response.Headers;
                }
                return _responseHeaders;
            }
            set { _responseHeaders = value; }
        }

        bool IHttpResponseFeature.HasStarted
        {
            get { return Response.HasStarted; }
        }

        void IHttpResponseFeature.OnResponseStarting(Func<object, Task> callback, object state)
        {
            Response.OnResponseStarting(callback, state);
        }

        void IHttpResponseFeature.OnResponseCompleted(Func<object, Task> callback, object state)
        {
            Response.OnResponseCompleted(callback, state);
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get { return Response.ReasonPhrase; }
            set { Response.ReasonPhrase = value; }
        }

        int IHttpResponseFeature.StatusCode
        {
            get { return Response.StatusCode; }
            set { Response.StatusCode = value; }
        }

        Task IHttpSendFileFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            return Response.SendFileAsync(path, offset, length, cancellation);
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get
            {
                if (!_disconnectToken.HasValue)
                {
                    _disconnectToken = _requestContext.DisconnectToken;
                }
                return _disconnectToken.Value;
            }
            set { _disconnectToken = value; }
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            _requestContext.Abort();
        }

        bool IHttpUpgradeFeature.IsUpgradableRequest
        {
            get { return _requestContext.IsUpgradableRequest; }
        }

        Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            return _requestContext.UpgradeAsync();
        }

        bool IHttpWebSocketFeature.IsWebSocketRequest
        {
            get
            {
                return _requestContext.IsWebSocketRequest;
            }
        }

        Task<WebSocket> IHttpWebSocketFeature.AcceptAsync(WebSocketAcceptContext context)
        {
            // TODO: Advanced params
            string subProtocol = null;
            if (context != null)
            {
                subProtocol = context.SubProtocol;
            }
            return _requestContext.AcceptWebSocketAsync(subProtocol);
        }

        ClaimsPrincipal IHttpAuthenticationFeature.User
        {
            get
            {
                if (_user == null)
                {
                    _user = _requestContext.User;
                }
                return _user;
            }
            set { _user = value; }
        }

        IAuthenticationHandler IHttpAuthenticationFeature.Handler
        {
            get { return _authHandler; }
            set { _authHandler = value; }
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get
            {
                if (_requestId == null)
                {
                    _requestId = _requestContext.TraceIdentifier.ToString();
                }
                return _requestId;
            }
            set { _requestId = value; }
        }

        private static Task OnStart(object obj)
        {
            var featureContext = (FeatureContext)obj;

            ConsiderEnablingResponseCache(featureContext);
            return Task.FromResult(0);
        }

        private static void ConsiderEnablingResponseCache(FeatureContext featureContext)
        {
            if (featureContext._enableResponseCaching)
            {
                // We don't have to worry too much about what Http.Sys supports, caching is a best-effort feature.
                // If there's something about the request or response that prevents it from caching then the response
                // will complete normally without caching.
                featureContext._requestContext.Response.CacheTtl = GetCacheTtl(featureContext._requestContext);
            }
        }

        private static TimeSpan? GetCacheTtl(RequestContext requestContext)
        {
            var response = requestContext.Response;
            // Only consider kernel-mode caching if the Cache-Control response header is present.
            var cacheControlHeader = response.Headers[HeaderNames.CacheControl];
            if (string.IsNullOrEmpty(cacheControlHeader))
            {
                return null;
            }

            // Before we check the header value, check for the existence of other headers which would
            // make us *not* want to cache the response.
            if (response.Headers.ContainsKey(HeaderNames.SetCookie)
                || response.Headers.ContainsKey(HeaderNames.Vary)
                || response.Headers.ContainsKey(HeaderNames.Pragma))
            {
                return null;
            }

            // We require 'public' and 's-max-age' or 'max-age' or the Expires header.
            CacheControlHeaderValue cacheControl;
            if (CacheControlHeaderValue.TryParse(cacheControlHeader, out cacheControl) && cacheControl.Public)
            {
                if (cacheControl.SharedMaxAge.HasValue)
                {
                    return cacheControl.SharedMaxAge;
                }
                else if (cacheControl.MaxAge.HasValue)
                {
                    return cacheControl.MaxAge;
                }

                DateTimeOffset expirationDate;
                if (HeaderUtilities.TryParseDate(response.Headers[HeaderNames.Expires], out expirationDate))
                {
                    var expiresOffset = expirationDate - DateTimeOffset.UtcNow;
                    if (expiresOffset > TimeSpan.Zero)
                    {
                        return expiresOffset;
                    }
                }
            }

            return null;
        }
    }
}
