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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebSockets
{
    using WebSocketCloseAsync =
        Func<int /* closeStatus */,
            string /* closeDescription */,
            CancellationToken /* cancel */,
            Task>;
    using WebSocketReceiveAsync =
        Func<ArraySegment<byte> /* data */,
            CancellationToken /* cancel */,
            Task<Tuple<int /* messageType */,
                bool /* endOfMessage */,
                int /* count */>>>;
    using WebSocketReceiveTuple =
        Tuple<int /* messageType */,
            bool /* endOfMessage */,
            int /* count */>;
    using WebSocketSendAsync =
        Func<ArraySegment<byte> /* data */,
            int /* messageType */,
            bool /* endOfMessage */,
            CancellationToken /* cancel */,
            Task>;

    internal class OwinWebSocketWrapper
    {
        private readonly WebSocket _webSocket;
        private readonly IDictionary<string, object> _environment;
        private readonly CancellationToken _cancellationToken;

        internal OwinWebSocketWrapper(WebSocket webSocket, CancellationToken ct)
        {
            _webSocket = webSocket;
            _cancellationToken = ct;

            _environment = new Dictionary<string, object>();
            _environment[Constants.WebSocketSendAsyncKey] = new WebSocketSendAsync(SendAsync);
            _environment[Constants.WebSocketReceiveAyncKey] = new WebSocketReceiveAsync(ReceiveAsync);
            _environment[Constants.WebSocketCloseAsyncKey] = new WebSocketCloseAsync(CloseAsync);
            _environment[Constants.WebSocketCallCancelledKey] = ct;
            _environment[Constants.WebSocketVersionKey] = Constants.WebSocketVersion;
        }

        internal IDictionary<string, object> Environment
        {
            get { return _environment; }
        }

        internal Task SendAsync(ArraySegment<byte> buffer, int messageType, bool endOfMessage, CancellationToken cancel)
        {
            // Remap close messages to CloseAsync.  System.Net.WebSockets.WebSocket.SendAsync does not allow close messages.
            if (messageType == 0x8)
            {
                return RedirectSendToCloseAsync(buffer, cancel);
            }
            else if (messageType == 0x9 || messageType == 0xA)
            {
                // Ping & Pong, not allowed by the underlying APIs, silently discard.
                return Task.FromResult(0);
            }

            return _webSocket.SendAsync(buffer, (WebSocketMessageType)messageType, endOfMessage, cancel);
        }

        internal async Task<WebSocketReceiveTuple> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancel)
        {
            WebSocketReceiveResult nativeResult = await _webSocket.ReceiveAsync(buffer, cancel);

            if (nativeResult.MessageType == WebSocketMessageType.Close)
            {
                _environment[Constants.WebSocketCloseStatusKey] = (int)(nativeResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure);
                _environment[Constants.WebSocketCloseDescriptionKey] = nativeResult.CloseStatusDescription ?? string.Empty;
            }

            return new WebSocketReceiveTuple(
                (int)nativeResult.MessageType,
                nativeResult.EndOfMessage,
                nativeResult.Count);
        }

        internal Task CloseAsync(int status, string description, CancellationToken cancel)
        {
            return _webSocket.CloseOutputAsync((WebSocketCloseStatus)status, description, cancel);
        }

        private Task RedirectSendToCloseAsync(ArraySegment<byte> buffer, CancellationToken cancel)
        {
            if (buffer.Array == null || buffer.Count == 0)
            {
                return CloseAsync(1000, string.Empty, cancel);
            }
            else if (buffer.Count >= 2)
            {
                // Unpack the close message.
                int statusCode =
                    (buffer.Array[buffer.Offset] << 8)
                        | buffer.Array[buffer.Offset + 1];
                string description = Encoding.UTF8.GetString(buffer.Array, buffer.Offset + 2, buffer.Count - 2);

                return CloseAsync(statusCode, description, cancel);
            }
            else
            {
                throw new ArgumentOutOfRangeException("buffer");
            }
        }

        internal async Task CleanupAsync()
        {
            switch (_webSocket.State)
            {
                case WebSocketState.Closed: // Closed gracefully, no action needed. 
                case WebSocketState.Aborted: // Closed abortively, no action needed.                       
                    break;
                case WebSocketState.CloseReceived:
                    // Echo what the client said, if anything.
                    await _webSocket.CloseAsync(_webSocket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        _webSocket.CloseStatusDescription ?? string.Empty, _cancellationToken);
                    break;
                case WebSocketState.Open:
                case WebSocketState.CloseSent: // No close received, abort so we don't have to drain the pipe.
                    _webSocket.Abort();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("state", _webSocket.State, string.Empty);
            }
        }
    }
}
