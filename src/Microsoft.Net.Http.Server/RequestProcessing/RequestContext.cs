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
// <copyright file="HttpListenerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    public sealed class RequestContext : IDisposable
    {
        private static readonly Action<object> AbortDelegate = Abort;

        private NativeRequestContext _memoryBlob;
        private CancellationTokenSource _requestAbortSource;
        private CancellationToken? _disconnectToken;
        private bool _disposed;

        internal RequestContext(WebListener server, NativeRequestContext memoryBlob)
        {
            // TODO: Verbose log
            Server = server;
            _memoryBlob = memoryBlob;
            Request = new Request(this, _memoryBlob);
            Response = new Response(this);
        }

        internal WebListener Server { get; }

        internal ILogger Logger => Server.Logger;

        public Request Request { get; }

        public Response Response { get; }

        public ClaimsPrincipal User => Request.User;

        public CancellationToken DisconnectToken
        {
            get
            {
                // Create a new token per request, but link it to a single connection token.
                // We need to be able to dispose of the registrations each request to prevent leaks.
                if (!_disconnectToken.HasValue)
                {
                    var connectionDisconnectToken = Server.DisconnectListener.GetTokenForConnection(Request.UConnectionId);

                    if (connectionDisconnectToken.CanBeCanceled)
                    {
                        _requestAbortSource = CancellationTokenSource.CreateLinkedTokenSource(connectionDisconnectToken);
                        _disconnectToken = _requestAbortSource.Token;
                    }
                    else
                    {
                        _disconnectToken = CancellationToken.None;
                    }
                }
                return _disconnectToken.Value;
            }
        }

        public unsafe Guid TraceIdentifier
        {
            get
            {
                // This is the base GUID used by HTTP.SYS for generating the activity ID.
                // HTTP.SYS overwrites the first 8 bytes of the base GUID with RequestId to generate ETW activity ID.
                var guid = new Guid(0xffcb4c93, 0xa57f, 0x453c, 0xb6, 0x3f, 0x84, 0x71, 0xc, 0x79, 0x67, 0xbb);
                *((ulong*)&guid) = Request.RequestId;
                return guid;
            }
        }

        public bool IsUpgradableRequest => Request.IsUpgradable;

        public Task<Stream> UpgradeAsync()
        {
            if (!IsUpgradableRequest)
            {
                throw new InvalidOperationException("This request cannot be upgraded, it is incompatible.");
            }
            if (Response.HasStarted)
            {
                throw new InvalidOperationException("This request cannot be upgraded, the response has already started.");
            }

            // Set the status code and reason phrase
            Response.StatusCode = (int)HttpStatusCode.SwitchingProtocols;
            Response.ReasonPhrase = HttpReasonPhrase.Get(HttpStatusCode.SwitchingProtocols);

            Response.SendOpaqueUpgrade(); // TODO: Async
            Request.SwitchToOpaqueMode();
            Response.SwitchToOpaqueMode();
            var opaqueStream = new OpaqueStream(Request.Body, Response.Body);
            return Task.FromResult<Stream>(opaqueStream);
        }

        // TODO: Public when needed
        internal bool TryGetChannelBinding(ref ChannelBinding value)
        {
            if (!Request.IsHttps)
            {
                LogHelper.LogDebug(Logger, "TryGetChannelBinding", "Channel binding requires HTTPS.");
                return false;
            }

            value = ClientCertLoader.GetChannelBindingFromTls(Server.RequestQueue, Request.UConnectionId, Logger);

            Debug.Assert(value != null, "GetChannelBindingFromTls returned null even though OS supposedly supports Extended Protection");
            LogHelper.LogInfo(Logger, "Channel binding retrieved.");
            return value != null;
        }

        /// <summary>
        /// Flushes and completes the response.
        /// </summary>
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
                if (_requestAbortSource != null)
                {
                    _requestAbortSource.Dispose();
                }
                Response.Dispose();
            }
            finally
            {
                Request.Dispose();
            }
        }

        /// <summary>
        /// Forcibly terminate and dispose the request, closing the connection if necessary.
        /// </summary>
        public void Abort()
        {
            // May be called from Dispose() code path, don't check _disposed.
            // TODO: Verbose log
            _disposed = true;
            if (_requestAbortSource != null)
            {
                try
                {
                    _requestAbortSource.Cancel();
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(Logger, "Abort", ex);
                }
                _requestAbortSource.Dispose();
            }
            ForceCancelRequest();
            Request.Dispose();
        }

        private static void Abort(object state)
        {
            var context = (RequestContext)state;
            context.Abort();
        }

        internal CancellationTokenRegistration RegisterForCancellation(CancellationToken cancellationToken)
        {
            return cancellationToken.Register(AbortDelegate, this);
        }

        // The request is being aborted, but large writes may be in progress. Cancel them.
        internal void ForceCancelRequest()
        {
            try
            {
                var statusCode = UnsafeNclNativeMethods.HttpApi.HttpCancelHttpRequest(Server.RequestQueue.Handle,
                    Request.RequestId, IntPtr.Zero);

                // Either the connection has already dropped, or the last write is in progress.
                // The requestId becomes invalid as soon as the last Content-Length write starts.
                // The only way to cancel now is with CancelIoEx.
                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_CONNECTION_INVALID)
                {
                    Response.CancelLastWrite();
                }
            }
            catch (ObjectDisposedException)
            {
                // RequestQueueHandle may have been closed
            }
        }
    }
}
