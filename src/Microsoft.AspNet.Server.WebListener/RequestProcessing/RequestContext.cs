//------------------------------------------------------------------------------
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Logging;

namespace Microsoft.AspNet.Server.WebListener
{
    using OpaqueFunc = Func<IDictionary<string, object>, Task>;

    internal sealed class RequestContext : IDisposable
    {
        private static readonly string[] ZeroContentLength = new[] { "0" };

        private OwinWebListener _server;
        private Request _request;
        private Response _response;
        private CancellationTokenSource _cts;
        private NativeRequestContext _memoryBlob;
        private OpaqueFunc _opaqueCallback;
        private bool _disposed;
        private CancellationTokenRegistration? _disconnectRegistration;
        private CancellationToken? _disconnectToken;

        internal RequestContext(OwinWebListener httpListener, NativeRequestContext memoryBlob)
        {
            // TODO: Verbose log
            _server = httpListener;
            _memoryBlob = memoryBlob;
            _request = new Request(this, _memoryBlob);
            _response = new Response(this);
            _cts = new CancellationTokenSource();
            _request.ReleasePins();
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

        internal CancellationToken DisconnectToken
        {
            get
            {
                if (!_disconnectToken.HasValue)
                {
                    _disconnectToken = _server.RegisterForDisconnectNotification(this);
                    if (_disconnectToken.Value.CanBeCanceled)
                    {
                        _disconnectRegistration = _disconnectToken.Value.Register(Cancel, this);
                    }
                }
                return _disconnectToken.Value;
            }
        }

        private static void Cancel(object obj)
        {
            RequestContext context = (RequestContext)obj;
            context.Abort();
        }

        internal OwinWebListener Server
        {
            get
            {
                return _server;
            }
        }

        internal ILogger Logger
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
        /*
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
        */

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
                if (_disconnectRegistration.HasValue)
                {
                    _disconnectRegistration.Value.Dispose();
                }
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
            Response.StatusCode = (int)HttpStatusCode.SwitchingProtocols;
            Response.ReasonPhrase = HttpReasonPhrase.Get(HttpStatusCode.SwitchingProtocols);

            // Store the callback and process it after the stack unwind.
            _opaqueCallback = callback;
        }

        // Called after the AppFunc completes for any necessary post-processing.
        internal unsafe Task ProcessResponseAsync()
        {
            // If an upgrade was requested, perform it
            if (!Response.SentHeaders && _opaqueCallback != null
                && Response.StatusCode == (int)HttpStatusCode.SwitchingProtocols)
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
            // opaqueEnv[Constants.OpaqueCallCancelledKey] = Environment.CallCancelled;

            Request.SwitchToOpaqueMode();
            Response.SwitchToOpaqueMode();
            opaqueEnv[Constants.OpaqueStreamKey] = new OpaqueStream(Request.Body, Response.Body);

            return opaqueEnv;
        }

        internal void SetFatalResponse()
        {
            Response.StatusCode = 500;
            Response.ReasonPhrase = string.Empty;
            Response.Headers.Clear();
            Response.Headers.Add(HttpKnownHeaderNames.ContentLength, ZeroContentLength);
        }
    }
}
