//------------------------------------------------------------------------------
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.WebListener
{
    using LoggerFunc = Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool>;
    using OpaqueFunc = Func<IDictionary<string, object>, Task>;

    internal sealed class RequestContext : IDisposable, CallEnvironment.IPropertySource
    {
        private static readonly string[] ZeroContentLength = new[] { "0" };

        private CallEnvironment _environment;
        private OwinWebListener _server;
        private Request _request;
        private Response _response;
        private CancellationTokenSource _cts;
        private NativeRequestContext _memoryBlob;
        private OpaqueFunc _opaqueCallback;
        private bool _disposed;

        internal RequestContext(OwinWebListener httpListener, NativeRequestContext memoryBlob)
        {
            // TODO: Verbose log
            _server = httpListener;
            _memoryBlob = memoryBlob;
            _request = new Request(this, _memoryBlob);
            _response = new Response(this);
            _environment = new CallEnvironment(this);
            _cts = new CancellationTokenSource();

            PopulateEnvironment();

            _request.ReleasePins();
        }

        internal CallEnvironment Environment
        {
            get { return _environment; }
        }

        internal Request Request
        {
            get
            {
                return _request;
            }
        }

        internal Response Response
        {
            get
            {
                return _response;
            }
        }

        internal OwinWebListener Server
        {
            get
            {
                return _server;
            }
        }

        internal LoggerFunc Logger
        {
            get { return Server.Logger; }
        }

        internal SafeHandle RequestQueueHandle
        {
            get
            {
                return _server.RequestQueueHandle;
            }
        }

        internal ulong RequestId
        {
            get
            {
                return Request.RequestId;
            }
        }

        private void PopulateEnvironment()
        {
            // General
            _environment.OwinVersion = Constants.OwinVersion;
            _environment.CallCancelled = _cts.Token;

            // Server
            _environment.ServerCapabilities = _server.Capabilities;
            _environment.Listener = _server;

            // Request
            _environment.RequestProtocol = _request.Protocol;
            _environment.RequestMethod = _request.HttpMethod;
            _environment.RequestScheme = _request.RequestScheme;
            _environment.RequestQueryString = _request.Query;
            _environment.RequestHeaders = _request.Headers;

            SetPaths();

            _environment.ConnectionId = _request.ConnectionId;

            if (_request.IsSecureConnection)
            {
                _environment.LoadClientCert = LoadClientCertificateAsync;
            }

            if (_request.User != null)
            {
                _environment.User = _request.User;
            }

            // Response
            _environment.ResponseStatusCode = 200;
            _environment.ResponseHeaders = _response.Headers;
            _environment.ResponseBody = _response.OutputStream;
            _environment.SendFileAsync = _response.SendFileAsync;

            _environment.OnSendingHeaders = _response.RegisterForOnSendingHeaders;

            Contract.Assert(!_environment.IsExtraDictionaryCreated,
                "All server keys should have a reserved slot in the environment.");
        }

        // Find the closest matching prefix and use it to separate the request path in to path and base path.
        // Scheme and port must match. Path will use a longest match. Host names are more complicated due to
        // wildcards, IP addresses, etc.
        private void SetPaths()
        {
            Prefix prefix = _server.UriPrefixes[(int)Request.ContextId];
            string orriginalPath = _request.RequestPath;

            // These paths are both unescaped already.
            if (orriginalPath.Length == prefix.Path.Length - 1)
            {
                // They matched exactly except for the trailing slash.
                _environment.RequestPathBase = orriginalPath;
                _environment.RequestPath = string.Empty;
            }
            else
            {
                // url: /base/path, prefix: /base/, base: /base, path: /path
                // url: /, prefix: /, base: , path: /
                _environment.RequestPathBase = orriginalPath.Substring(0, prefix.Path.Length - 1);
                _environment.RequestPath = orriginalPath.Substring(prefix.Path.Length - 1);
            }
        }

        // Lazy environment init

        public Stream GetRequestBody()
        {
            return _request.InputStream;
        }

        public string GetRemoteIpAddress()
        {
            return _request.RemoteEndPoint.GetIPAddressString();
        }

        public string GetRemotePort()
        {
            return _request.RemoteEndPoint.GetPort().ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        public string GetLocalIpAddress()
        {
            return _request.LocalEndPoint.GetIPAddressString();
        }

        public string GetLocalPort()
        {
            return _request.LocalEndPoint.GetPort().ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        public bool GetIsLocal()
        {
            return _request.IsLocal;
        }

        public bool TryGetOpaqueUpgrade(ref Action<IDictionary<string, object>, OpaqueFunc> value)
        {
            if (_request.IsUpgradable)
            {
                value = OpaqueUpgrade;
                return true;
            }
            return false;
        }

        public bool TryGetChannelBinding(ref ChannelBinding value)
        {
            value = Server.GetChannelBinding(Request.ConnectionId, Request.IsSecureConnection);
            return value != null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            // TODO: Verbose log
            try
            {
                _response.Dispose();
            }
            finally
            {
                _request.Dispose();
                _cts.Dispose();
            }
        }

        internal void Abort()
        {
            // TODO: Verbose log
            _disposed = true;
            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (AggregateException)
            {
            }
            ForceCancelRequest(RequestQueueHandle, _request.RequestId);
            _request.Dispose();
            _cts.Dispose();
        }

        internal UnsafeNclNativeMethods.HttpApi.HTTP_VERB GetKnownMethod()
        {
            return UnsafeNclNativeMethods.HttpApi.GetKnownVerb(Request.RequestBuffer, Request.OriginalBlobAddress);
        }

        // This is only called while processing incoming requests.  We don't have to worry about cancelling 
        // any response writes.
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Justification =
            "It is safe to ignore the return value on a cancel operation because the connection is being closed")]
        internal static void CancelRequest(SafeHandle requestQueueHandle, ulong requestId)
        {
            UnsafeNclNativeMethods.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId,
                IntPtr.Zero);
        }

        // The request is being aborted, but large writes may be in progress. Cancel them.
        internal void ForceCancelRequest(SafeHandle requestQueueHandle, ulong requestId)
        {
            try
            {
                uint statusCode = UnsafeNclNativeMethods.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId,
                    IntPtr.Zero);

                // Either the connection has already dropped, or the last write is in progress.
                // The requestId becomes invalid as soon as the last Content-Length write starts.
                // The only way to cancel now is with CancelIoEx.
                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_CONNECTION_INVALID)
                {
                    _response.CancelLastWrite(requestQueueHandle);
                }
            }
            catch (ObjectDisposedException)
            {
                // RequestQueueHandle may have been closed
            }
        }

        // Populates the environment ClicentCertificate.  The result may be null if there is no client cert.
        // TODO: Does it make sense for this to be invoked multiple times (e.g. renegotiate)? Client and server code appear to
        // enable this, but it's unclear what Http.Sys would do.
        private async Task LoadClientCertificateAsync()
        {
            if (Request.SslStatus == SslStatus.Insecure)
            {
                // Non-SSL
                return;
            }
            // TODO: Verbose log
#if NET45
            ClientCertLoader certLoader = new ClientCertLoader(this);
            try
            {
                await certLoader.LoadClientCertificateAsync().SupressContext();
                // Populate the environment.
                if (certLoader.ClientCert != null)
                {
                    Environment.ClientCert = certLoader.ClientCert;
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

        internal void OpaqueUpgrade(IDictionary<string, object> parameters, OpaqueFunc callback)
        {
            // Parameters are ignored for now
            if (Response.SentHeaders)
            {
                throw new InvalidOperationException();
            }
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            // Set the status code and reason phrase
            Environment.ResponseStatusCode = (int)HttpStatusCode.SwitchingProtocols;
            Environment.ResponseReasonPhrase = HttpReasonPhrase.Get(HttpStatusCode.SwitchingProtocols);

            // Store the callback and process it after the stack unwind.
            _opaqueCallback = callback;
        }

        // Called after the AppFunc completes for any necessary post-processing.
        internal unsafe Task ProcessResponseAsync()
        {
            // If an upgrade was requested, perform it
            if (!Response.SentHeaders && _opaqueCallback != null
                && Environment.ResponseStatusCode == (int)HttpStatusCode.SwitchingProtocols)
            {
                Response.SendOpaqueUpgrade();

                IDictionary<string, object> opaqueEnv = CreateOpaqueEnvironment();
                return _opaqueCallback(opaqueEnv);
            }

            return Helpers.CompletedTask();
        }

        private IDictionary<string, object> CreateOpaqueEnvironment()
        {
            IDictionary<string, object> opaqueEnv = new Dictionary<string, object>();

            opaqueEnv[Constants.OpaqueVersionKey] = Constants.OpaqueVersion;
            // TODO: Separate CT?
            opaqueEnv[Constants.OpaqueCallCancelledKey] = Environment.CallCancelled;

            Request.SwitchToOpaqueMode();
            Response.SwitchToOpaqueMode();
            opaqueEnv[Constants.OpaqueStreamKey] = new OpaqueStream(Request.InputStream, Response.OutputStream);

            return opaqueEnv;
        }

        internal void SetFatalResponse()
        {
            Environment.ResponseStatusCode = 500;
            Environment.ResponseReasonPhrase = string.Empty;
            Environment.ResponseHeaders.Clear();
            Environment.ResponseHeaders.Add(HttpKnownHeaderNames.ContentLength, ZeroContentLength);
        }
    }
}
