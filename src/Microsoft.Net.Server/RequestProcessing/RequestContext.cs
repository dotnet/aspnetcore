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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using Microsoft.Net.WebSockets;

namespace Microsoft.Net.Server
{
    public sealed class RequestContext : IDisposable
    {
        private WebListener _server;
        private Request _request;
        private Response _response;
        private NativeRequestContext _memoryBlob;
        private bool _disposed;
        private CancellationTokenSource _requestAbortSource;
        private CancellationToken? _disconnectToken;

        internal RequestContext(WebListener server, NativeRequestContext memoryBlob)
        {
            // TODO: Verbose log
            _server = server;
            _memoryBlob = memoryBlob;
            _request = new Request(this, _memoryBlob);
            _response = new Response(this);
            _request.ReleasePins();
            AuthenticationChallenges = server.AuthenticationManager.AuthenticationTypes & ~AuthenticationTypes.AllowAnonymous;
        }

        public Request Request
        {
            get
            {
                return _request;
            }
        }

        public Response Response
        {
            get
            {
                return _response;
            }
        }

        public ClaimsPrincipal User
        {
            get { return _request.User; }
        }

        public CancellationToken DisconnectToken
        {
            get
            {
                // Create a new token per request, but link it to a single connection token.
                // We need to be able to dispose of the registrations each request to prevent leaks.
                if (!_disconnectToken.HasValue)
                {
                    var connectionDisconnectToken = _server.RegisterForDisconnectNotification(this);

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

        internal WebListener Server
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

        /// <summary>
        /// The authentication challengest that will be added to the response if the status code is 401.
        /// This must be a subset of the AuthenticationTypes enabled on the server.
        /// </summary>
        public AuthenticationTypes AuthenticationChallenges { get; set; }

        public bool IsUpgradableRequest
        {
            get { return _request.IsUpgradable; }
        }

        public Task<Stream> UpgradeAsync()
        {
            if (!IsUpgradableRequest || _response.SentHeaders)
            {
                throw new InvalidOperationException();
            }

            // Set the status code and reason phrase
            Response.StatusCode = (int)HttpStatusCode.SwitchingProtocols;
            Response.ReasonPhrase = HttpReasonPhrase.Get(HttpStatusCode.SwitchingProtocols);

            Response.SendOpaqueUpgrade(); // TODO: Async
            Request.SwitchToOpaqueMode();
            Response.SwitchToOpaqueMode();
            Stream opaqueStream = new OpaqueStream(Request.Body, Response.Body);
            return Task.FromResult(opaqueStream);
        }

        public bool IsWebSocketRequest
        {
            get
            {
                if (!WebSocketHelpers.AreWebSocketsSupported)
                {
                    return false;
                }

                if (!IsUpgradableRequest)
                {
                    return false;
                }

                if (!string.Equals("GET", Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Connection: Upgrade (some odd clients send Upgrade,KeepAlive)
                string connection = Request.GetHeader(HttpKnownHeaderNames.Connection);
                if (connection.IndexOf(HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                // Upgrade: websocket
                string upgrade = Request.GetHeader(HttpKnownHeaderNames.Upgrade);
                if (!string.Equals(WebSocketHelpers.WebSocketUpgradeToken, upgrade, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Sec-WebSocket-Version: 13
                string version = Request.GetHeader(HttpKnownHeaderNames.SecWebSocketVersion);
                if (!string.Equals(WebSocketConstants.SupportedProtocolVersion, version, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Sec-WebSocket-Key: {base64string}
                string key = Request.GetHeader(HttpKnownHeaderNames.SecWebSocketKey);
                if (!WebSocketHelpers.IsValidWebSocketKey(key))
                {
                    return false;
                }

                return true;
            }
        }

        // Compare IsWebSocketRequest
        private void ValidateWebSocketRequest()
        {
            if (!WebSocketHelpers.AreWebSocketsSupported)
            {
                throw new NotSupportedException("WebSockets are not supported on this platform.");
            }

            if (!IsUpgradableRequest)
            {
                throw new InvalidOperationException("This request is not a valid upgrade request.");
            }

            if (!string.Equals("GET", Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This request is not a valid upgrade request; invalid verb: " + Request.Method);
            }

            // Connection: Upgrade (some odd clients send Upgrade,KeepAlive)
            string connection = Request.GetHeader(HttpKnownHeaderNames.Connection);
            if (connection.IndexOf(HttpKnownHeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("The Connection header is invalid: " + connection);
            }

            // Upgrade: websocket
            string upgrade = Request.GetHeader(HttpKnownHeaderNames.Upgrade);
            if (!string.Equals(WebSocketHelpers.WebSocketUpgradeToken, upgrade, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Upgrade header is invalid: " + upgrade);
            }

            // Sec-WebSocket-Version: 13
            string version = Request.GetHeader(HttpKnownHeaderNames.SecWebSocketVersion);
            if (!string.Equals(WebSocketConstants.SupportedProtocolVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Sec-WebSocket-Version header is invalid or not supported: " + version);
            }

            // Sec-WebSocket-Key: {base64string}
            string key = Request.GetHeader(HttpKnownHeaderNames.SecWebSocketKey);
            if (!WebSocketHelpers.IsValidWebSocketKey(key))
            {
                throw new InvalidOperationException("The Sec-WebSocket-Key header is invalid: " + upgrade);
            }
        }

        public Task<WebSocket> AcceptWebSocketAsync()
        {
            return AcceptWebSocketAsync(null,
                WebSocketHelpers.DefaultReceiveBufferSize,
                WebSocketHelpers.DefaultKeepAliveInterval);
        }

        public Task<WebSocket> AcceptWebSocketAsync(string subProtocol)
        {
            return AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                WebSocketHelpers.DefaultKeepAliveInterval);
        }

        public Task<WebSocket> AcceptWebSocketAsync(string subProtocol, TimeSpan keepAliveInterval)
        {
            return AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                keepAliveInterval);
        }

        public Task<WebSocket> AcceptWebSocketAsync(
            string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval)
        {
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);

            ArraySegment<byte> internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);
            return this.AcceptWebSocketAsync(subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }

        public Task<WebSocket> AcceptWebSocketAsync(
                    string subProtocol,
                    int receiveBufferSize,
                    TimeSpan keepAliveInterval,
                    ArraySegment<byte> internalBuffer)
        {
            if (!IsUpgradableRequest)
            {
                throw new InvalidOperationException("This request is cannot be upgraded.");
            }
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);
            WebSocketHelpers.ValidateArraySegment<byte>(internalBuffer, "internalBuffer");
            WebSocketBuffer.Validate(internalBuffer.Count, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);

            return AcceptWebSocketAsyncCore(subProtocol, receiveBufferSize, keepAliveInterval, internalBuffer);
        }

        private async Task<WebSocket> AcceptWebSocketAsyncCore(
                    string subProtocol,
                    int receiveBufferSize,
                    TimeSpan keepAliveInterval,
                    ArraySegment<byte> internalBuffer)
        {
            try
            {
                // TODO: We need a better header collection API.
                ValidateWebSocketRequest();

                string subProtocols = string.Empty;
                string[] values;
                if (Request.Headers.TryGetValue(HttpKnownHeaderNames.SecWebSocketProtocol, out values))
                {
                    subProtocols = string.Join(", ", values);
                }

                bool shouldSendSecWebSocketProtocolHeader = WebSocketHelpers.ProcessWebSocketProtocolHeader(subProtocols, subProtocol);
                if (shouldSendSecWebSocketProtocolHeader)
                {
                    Response.Headers[HttpKnownHeaderNames.SecWebSocketProtocol] = new[] { subProtocol };
                }

                // negotiate the websocket key return value
                string secWebSocketKey = Request.Headers[HttpKnownHeaderNames.SecWebSocketKey].First();
                string secWebSocketAccept = WebSocketHelpers.GetSecWebSocketAcceptString(secWebSocketKey);

                Response.Headers.Add(HttpKnownHeaderNames.Connection, new[] { HttpKnownHeaderNames.Upgrade });
                Response.Headers.Add(HttpKnownHeaderNames.Upgrade, new[] { WebSocketHelpers.WebSocketUpgradeToken });
                Response.Headers.Add(HttpKnownHeaderNames.SecWebSocketAccept, new[] { secWebSocketAccept });

                Stream opaqueStream = await UpgradeAsync();

                return WebSocketHelpers.CreateServerWebSocket(
                    opaqueStream,
                    subProtocol,
                    receiveBufferSize,
                    keepAliveInterval,
                    internalBuffer);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(Logger, "AcceptWebSocketAsync", ex);
                throw;
            }
        }


        /*
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
                if (_requestAbortSource != null)
                {
                    _requestAbortSource.Dispose();
                }
                _response.Dispose();
            }
            finally
            {
                _request.Dispose();
            }
        }

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
            ForceCancelRequest(RequestQueueHandle, _request.RequestId);
            _request.Dispose();
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
    }
}
