// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class FeatureContext :
        IHttpRequestFeature,
        IHttpConnectionFeature,
        IHttpResponseFeature,
        IHttpResponseBodyFeature,
        ITlsConnectionFeature,
        ITlsHandshakeFeature,
        // ITlsTokenBindingFeature, TODO: https://github.com/aspnet/HttpSysServer/issues/231
        IHttpRequestLifetimeFeature,
        IHttpAuthenticationFeature,
        IHttpUpgradeFeature,
        IHttpRequestIdentifierFeature,
        IHttpMaxRequestBodySizeFeature,
        IHttpBodyControlFeature,
        IHttpSysRequestInfoFeature,
        IHttpResponseTrailersFeature,
        IHttpResetFeature
    {
        private RequestContext _requestContext;
        private IFeatureCollection _features;
        private bool _enableResponseCaching;

        private Stream _requestBody;
        private IHeaderDictionary _requestHeaders;
        private string _scheme;
        private string _httpMethod;
        private string _httpProtocolVersion;
        private string _query;
        private string _pathBase;
        private string _path;
        private string _rawTarget;
        private IPAddress _remoteIpAddress;
        private IPAddress _localIpAddress;
        private int _remotePort;
        private int _localPort;
        private string _connectionId;
        private string _traceIdentitfier;
        private X509Certificate2 _clientCert;
        private ClaimsPrincipal _user;
        private CancellationToken _disconnectToken;
        private Stream _responseStream;
        private PipeWriter _pipeWriter;
        private bool _bodyCompleted;
        private IHeaderDictionary _responseHeaders;
        private IHeaderDictionary _responseTrailers;

        private Fields _initializedFields;

        private List<Tuple<Func<object, Task>, object>> _onStartingActions = new List<Tuple<Func<object, Task>, object>>();
        private List<Tuple<Func<object, Task>, object>> _onCompletedActions = new List<Tuple<Func<object, Task>, object>>();
        private bool _responseStarted;
        private bool _completed;

        internal FeatureContext(RequestContext requestContext)
        {
            _requestContext = requestContext;
            _features = new FeatureCollection(new StandardFeatureCollection(this));
            _enableResponseCaching = _requestContext.Server.Options.EnableResponseCaching;

            // Pre-initialize any fields that are not lazy at the lower level.
            _requestHeaders = Request.Headers;
            _httpMethod = Request.Method;
            _path = Request.Path;
            _pathBase = Request.PathBase;
            _query = Request.QueryString;
            _rawTarget = Request.RawUrl;
            _scheme = Request.Scheme;

            if (requestContext.Server.Options.Authentication.AutomaticAuthentication)
            {
                _user = _requestContext.User;
            }

            _responseStream = new ResponseStream(requestContext.Response.Body, OnResponseStart);
            _responseHeaders = Response.Headers;
        }

        internal IFeatureCollection Features => _features;

        internal object RequestContext => _requestContext;

        private Request Request => _requestContext.Request;

        private Response Response => _requestContext.Response;

        [Flags]
        // Fields that may be lazy-initialized
        private enum Fields
        {
            None = 0x0,
            Protocol = 0x1,
            RequestBody = 0x2,
            RequestAborted = 0x4,
            LocalIpAddress = 0x8,
            RemoteIpAddress = 0x10,
            LocalPort = 0x20,
            RemotePort = 0x40,
            ConnectionId = 0x80,
            ClientCertificate = 0x100,
            TraceIdentifier = 0x200,
        }

        private bool IsNotInitialized(Fields field)
        {
            return (_initializedFields & field) != field;
        }

        private void SetInitialized(Fields field)
        {
            _initializedFields |= field;
        }

        Stream IHttpRequestFeature.Body
        {
            get
            {
                if (IsNotInitialized(Fields.RequestBody))
                {
                    _requestBody = Request.Body;
                    SetInitialized(Fields.RequestBody);
                }
                return _requestBody;
            }
            set
            {
                _requestBody = value;
                SetInitialized(Fields.RequestBody);
            }
        }

        IHeaderDictionary IHttpRequestFeature.Headers
        {
            get { return _requestHeaders; }
            set { _requestHeaders = value; }
        }

        string IHttpRequestFeature.Method
        {
            get { return _httpMethod; }
            set { _httpMethod = value; }
        }

        string IHttpRequestFeature.Path
        {
            get { return _path; }
            set { _path = value; }
        }

        string IHttpRequestFeature.PathBase
        {
            get { return _pathBase; }
            set { _pathBase = value; }
        }

        string IHttpRequestFeature.Protocol
        {
            get
            {
                if (IsNotInitialized(Fields.Protocol))
                {
                    _httpProtocolVersion = HttpProtocol.GetHttpProtocol(Request.ProtocolVersion);
                    SetInitialized(Fields.Protocol);
                }
                return _httpProtocolVersion;
            }
            set
            {
                _httpProtocolVersion = value;
                SetInitialized(Fields.Protocol);
            }
        }

        string IHttpRequestFeature.QueryString
        {
            get { return _query; }
            set { _query = value; }
        }

        string IHttpRequestFeature.RawTarget
        {
            get { return _rawTarget; }
            set { _rawTarget = value; }
        }

        string IHttpRequestFeature.Scheme
        {
            get { return _scheme; }
            set { _scheme = value; }
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get
            {
                if (IsNotInitialized(Fields.LocalIpAddress))
                {
                    _localIpAddress = Request.LocalIpAddress;
                    SetInitialized(Fields.LocalIpAddress);
                }
                return _localIpAddress;
            }
            set
            {
                _localIpAddress = value;
                SetInitialized(Fields.LocalIpAddress);
            }
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get
            {
                if (IsNotInitialized(Fields.RemoteIpAddress))
                {
                    _remoteIpAddress = Request.RemoteIpAddress;
                    SetInitialized(Fields.RemoteIpAddress);
                }
                return _remoteIpAddress;
            }
            set
            {
                _remoteIpAddress = value;
                SetInitialized(Fields.RemoteIpAddress);
            }
        }

        int IHttpConnectionFeature.LocalPort
        {
            get
            {
                if (IsNotInitialized(Fields.LocalPort))
                {
                    _localPort = Request.LocalPort;
                    SetInitialized(Fields.LocalPort);
                }
                return _localPort;
            }
            set
            {
                _localPort = value;
                SetInitialized(Fields.LocalPort);
            }
        }

        int IHttpConnectionFeature.RemotePort
        {
            get
            {
                if (IsNotInitialized(Fields.RemotePort))
                {
                    _remotePort = Request.RemotePort;
                    SetInitialized(Fields.RemotePort);
                }
                return _remotePort;
            }
            set
            {
                _remotePort = value;
                SetInitialized(Fields.RemotePort);
            }
        }

        string IHttpConnectionFeature.ConnectionId
        {
            get
            {
                if (IsNotInitialized(Fields.ConnectionId))
                {
                    _connectionId = Request.ConnectionId.ToString(CultureInfo.InvariantCulture);
                    SetInitialized(Fields.ConnectionId);
                }
                return _connectionId;
            }
            set
            {
                _connectionId = value;
                SetInitialized(Fields.ConnectionId);
            }
        }

        X509Certificate2 ITlsConnectionFeature.ClientCertificate
        {
            get
            {
                if (IsNotInitialized(Fields.ClientCertificate))
                {
                    var method = _requestContext.Server.Options.ClientCertificateMethod;
                    if (method == ClientCertificateMethod.AllowCertificate)
                    {
                        _clientCert = Request.ClientCertificate;
                    }
                    else if (method == ClientCertificateMethod.AllowRenegotation)
                    {
                        _clientCert = Request.GetClientCertificateAsync().Result; // TODO: Sync over async;
                    }
                    // else if (method == ClientCertificateMethod.NoCertificate) // No-op

                    SetInitialized(Fields.ClientCertificate);
                }
                return _clientCert;
            }
            set
            {
                _clientCert = value;
                SetInitialized(Fields.ClientCertificate);
            }
        }

        async Task<X509Certificate2> ITlsConnectionFeature.GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            if (IsNotInitialized(Fields.ClientCertificate))
            {
                _clientCert = await Request.GetClientCertificateAsync(cancellationToken);
                SetInitialized(Fields.ClientCertificate);
            }
            return _clientCert;
        }

        internal ITlsConnectionFeature GetTlsConnectionFeature()
        {
            return Request.IsHttps ? this : null;
        }

        internal ITlsHandshakeFeature GetTlsHandshakeFeature()
        {
            return Request.IsHttps ? this : null;
        }

        internal IHttpResponseTrailersFeature GetResponseTrailersFeature()
        {
            if (Request.ProtocolVersion >= HttpVersion.Version20 && HttpApi.SupportsTrailers)
            {
                return this;
            }
            return null;
        }

        internal IHttpResetFeature GetResetFeature()
        {
            if (Request.ProtocolVersion >= HttpVersion.Version20 && HttpApi.SupportsReset)
            {
                return this;
            }
            return null;
        }

        /* TODO: https://github.com/aspnet/HttpSysServer/issues/231
        byte[] ITlsTokenBindingFeature.GetProvidedTokenBindingId() => Request.GetProvidedTokenBindingId();

        byte[] ITlsTokenBindingFeature.GetReferredTokenBindingId() => Request.GetReferredTokenBindingId();

        internal ITlsTokenBindingFeature GetTlsTokenBindingFeature()
        {
            return Request.IsHttps ? this : null;
        }
        */

        void IHttpResponseBodyFeature.DisableBuffering()
        {
            // TODO: What about native buffering?
        }

        Stream IHttpResponseFeature.Body
        {
            get { return _responseStream; }
            set { _responseStream = value; }
        }

        Stream IHttpResponseBodyFeature.Stream => _responseStream;

        PipeWriter IHttpResponseBodyFeature.Writer
        {
            get
            {
                if (_pipeWriter == null)
                {
                    _pipeWriter = PipeWriter.Create(_responseStream, new StreamPipeWriterOptions(leaveOpen: true));
                }

                return _pipeWriter;
            }
        }

        IHeaderDictionary IHttpResponseFeature.Headers
        {
            get { return _responseHeaders; }
            set { _responseHeaders = value; }
        }

        bool IHttpResponseFeature.HasStarted => _responseStarted;

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (_onStartingActions == null)
            {
                throw new InvalidOperationException("Cannot register new callbacks, the response has already started.");
            }

            _onStartingActions.Add(new Tuple<Func<object, Task>, object>(callback, state));
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
            if (_onCompletedActions == null)
            {
                throw new InvalidOperationException("Cannot register new callbacks, the response has already completed.");
            }

            _onCompletedActions.Add(new Tuple<Func<object, Task>, object>(callback, state));
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

        async Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            await OnResponseStart();
            await Response.SendFileAsync(path, offset, length, cancellation);
        }

        Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellation)
        {
            return OnResponseStart();
        }

        Task IHttpResponseBodyFeature.CompleteAsync() => CompleteAsync();

        void IHttpResetFeature.Reset(int errorCode)
        {
            _requestContext.SetResetCode(errorCode);
            _requestContext.Abort();
        }

        internal async Task CompleteAsync()
        {
            if (!_responseStarted)
            {
                await OnResponseStart();
            }

            if (!_bodyCompleted)
            {
                _bodyCompleted = true;
                if (_pipeWriter != null)
                {
                    // Flush and complete the pipe
                    await _pipeWriter.CompleteAsync();
                }

                // Ends the response body.
                Response.Dispose();
            }
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get
            {
                if (IsNotInitialized(Fields.RequestAborted))
                {
                    _disconnectToken = _requestContext.DisconnectToken;
                    SetInitialized(Fields.RequestAborted);
                }
                return _disconnectToken;
            }
            set
            {
                _disconnectToken = value;
                SetInitialized(Fields.RequestAborted);
            }
        }

        void IHttpRequestLifetimeFeature.Abort() => _requestContext.Abort();

        bool IHttpUpgradeFeature.IsUpgradableRequest => _requestContext.IsUpgradableRequest;

        async Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            await OnResponseStart();
            return await _requestContext.UpgradeAsync();
        }

        ClaimsPrincipal IHttpAuthenticationFeature.User
        {
            get { return _user; }
            set { _user = value; }
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get
            {
                if (IsNotInitialized(Fields.TraceIdentifier))
                {
                    _traceIdentitfier = _requestContext.TraceIdentifier.ToString();
                    SetInitialized(Fields.TraceIdentifier);
                }
                return _traceIdentitfier;
            }
            set
            {
                _traceIdentitfier = value;
                SetInitialized(Fields.TraceIdentifier);
            }
        }

        bool IHttpBodyControlFeature.AllowSynchronousIO
        {
            get => _requestContext.AllowSynchronousIO;
            set => _requestContext.AllowSynchronousIO = value;
        }

        bool IHttpMaxRequestBodySizeFeature.IsReadOnly => Request.HasRequestBodyStarted;

        long? IHttpMaxRequestBodySizeFeature.MaxRequestBodySize
        {
            get => Request.MaxRequestBodySize;
            set => Request.MaxRequestBodySize = value;
        }

        SslProtocols ITlsHandshakeFeature.Protocol => Request.Protocol;

        CipherAlgorithmType ITlsHandshakeFeature.CipherAlgorithm => Request.CipherAlgorithm;

        int ITlsHandshakeFeature.CipherStrength => Request.CipherStrength;

        HashAlgorithmType ITlsHandshakeFeature.HashAlgorithm => Request.HashAlgorithm;

        int ITlsHandshakeFeature.HashStrength => Request.HashStrength;

        ExchangeAlgorithmType ITlsHandshakeFeature.KeyExchangeAlgorithm => Request.KeyExchangeAlgorithm;

        int ITlsHandshakeFeature.KeyExchangeStrength => Request.KeyExchangeStrength;

        IReadOnlyDictionary<int, ReadOnlyMemory<byte>> IHttpSysRequestInfoFeature.RequestInfo => Request.RequestInfo;

        IHeaderDictionary IHttpResponseTrailersFeature.Trailers
        {
            get => _responseTrailers ??= Response.Trailers;
            set => _responseTrailers = value;
        }

        internal async Task OnResponseStart()
        {
            if (_responseStarted)
            {
                return;
            }
            _responseStarted = true;
            await NotifiyOnStartingAsync();
            ConsiderEnablingResponseCache();

            Response.Headers.IsReadOnly = true; // Prohibit further modifications.
        }

        private async Task NotifiyOnStartingAsync()
        {
            var actions = _onStartingActions;
            _onStartingActions = null;
            if (actions == null)
            {
                return;
            }

            actions.Reverse();
            // Execute last to first. This mimics a stack unwind.
            foreach (var actionPair in actions)
            {
                await actionPair.Item1(actionPair.Item2);
            }
        }

        private void ConsiderEnablingResponseCache()
        {
            if (_enableResponseCaching)
            {
                // We don't have to worry too much about what Http.Sys supports, caching is a best-effort feature.
                // If there's something about the request or response that prevents it from caching then the response
                // will complete normally without caching.
                _requestContext.Response.CacheTtl = GetCacheTtl(_requestContext);
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
            if (CacheControlHeaderValue.TryParse(cacheControlHeader.ToString(), out cacheControl) && cacheControl.Public)
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
                if (HeaderUtilities.TryParseDate(response.Headers[HeaderNames.Expires].ToString(), out expirationDate))
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

        internal Task OnCompleted()
        {
            if (_completed)
            {
                return Task.CompletedTask;
            }
            _completed = true;
            return NotifyOnCompletedAsync();
        }

        private async Task NotifyOnCompletedAsync()
        {
            var actions = _onCompletedActions;
            _onCompletedActions = null;
            if (actions == null)
            {
                return;
            }

            actions.Reverse();
            // Execute last to first. This mimics a stack unwind.
            foreach (var actionPair in actions)
            {
                await actionPair.Item1(actionPair.Item2);
            }
        }
    }
}
