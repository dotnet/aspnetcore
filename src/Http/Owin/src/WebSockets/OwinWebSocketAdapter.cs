// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.Owin
{
    // http://owin.org/extensions/owin-WebSocket-Extension-v0.4.0.htm
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
    using WebSocketSendAsync =
        Func<ArraySegment<byte> /* data */,
            int /* messageType */,
            bool /* endOfMessage */,
            CancellationToken /* cancel */,
            Task>;
    using RawWebSocketReceiveResult = Tuple<int, // type
        bool, // end of message?
        int>; // count

    public class OwinWebSocketAdapter : WebSocket
    {
        private const int _rentedBufferSize = 1024;
        private IDictionary<string, object> _websocketContext;
        private WebSocketSendAsync _sendAsync;
        private WebSocketReceiveAsync _receiveAsync;
        private WebSocketCloseAsync _closeAsync;
        private WebSocketState _state;
        private string _subProtocol;

        public OwinWebSocketAdapter(IDictionary<string, object> websocketContext, string subProtocol)
        {
            _websocketContext = websocketContext;
            _sendAsync = (WebSocketSendAsync)websocketContext[OwinConstants.WebSocket.SendAsync];
            _receiveAsync = (WebSocketReceiveAsync)websocketContext[OwinConstants.WebSocket.ReceiveAsync];
            _closeAsync = (WebSocketCloseAsync)websocketContext[OwinConstants.WebSocket.CloseAsync];
            _state = WebSocketState.Open;
            _subProtocol = subProtocol;
        }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                object obj;
                if (_websocketContext.TryGetValue(OwinConstants.WebSocket.ClientCloseStatus, out obj))
                {
                    return (WebSocketCloseStatus)obj;
                }
                return null;
            }
        }

        public override string CloseStatusDescription
        {
            get
            {
                object obj;
                if (_websocketContext.TryGetValue(OwinConstants.WebSocket.ClientCloseDescription, out obj))
                {
                    return (string)obj;
                }
                return null;
            }
        }

        public override string SubProtocol
        {
            get
            {
                return _subProtocol;
            }
        }

        public override WebSocketState State
        {
            get
            {
                return _state;
            }
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var rawResult = await _receiveAsync(buffer, cancellationToken);
            var messageType = OpCodeToEnum(rawResult.Item1);
            if (messageType == WebSocketMessageType.Close)
            {
                if (State == WebSocketState.Open)
                {
                    _state = WebSocketState.CloseReceived;
                }
                else if (State == WebSocketState.CloseSent)
                {
                    _state = WebSocketState.Closed;
                }
                return new WebSocketReceiveResult(rawResult.Item3, messageType, rawResult.Item2, CloseStatus, CloseStatusDescription);
            }
            else
            {
                return new WebSocketReceiveResult(rawResult.Item3, messageType, rawResult.Item2);
            }
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return _sendAsync(buffer, EnumToOpCode(messageType), endOfMessage, cancellationToken);
        }

        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
            {
                await CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            }

            var buffer = ArrayPool<byte>.Shared.Rent(_rentedBufferSize);
            try
            {
                while (State == WebSocketState.CloseSent)
                {
                    // Drain until close received
                    await ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            // TODO: Validate state
            if (State == WebSocketState.Open)
            {
                _state = WebSocketState.CloseSent;
            }
            else if (State == WebSocketState.CloseReceived)
            {
                _state = WebSocketState.Closed;
            }
            return _closeAsync((int)closeStatus, statusDescription, cancellationToken);
        }

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
        }

        private static WebSocketMessageType OpCodeToEnum(int messageType)
        {
            switch (messageType)
            {
                case 0x1:
                    return WebSocketMessageType.Text;
                case 0x2:
                    return WebSocketMessageType.Binary;
                case 0x8:
                    return WebSocketMessageType.Close;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageType), messageType, string.Empty);
            }
        }

        private static int EnumToOpCode(WebSocketMessageType webSocketMessageType)
        {
            switch (webSocketMessageType)
            {
                case WebSocketMessageType.Text:
                    return 0x1;
                case WebSocketMessageType.Binary:
                    return 0x2;
                case WebSocketMessageType.Close:
                    return 0x8;
                default:
                    throw new ArgumentOutOfRangeException(nameof(webSocketMessageType), webSocketMessageType, string.Empty);
            }
        }
    }
}