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
// <copyright file="WebSocketHelpers.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Microsoft.Net.Http.Server
{
    internal static class WebSocketHelpers
    {
        internal static string SupportedProtocolVersion = "13";

        internal const string SecWebSocketKeyGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        internal const string WebSocketUpgradeToken = "websocket";
        internal const int DefaultReceiveBufferSize = 16 * 1024;
        internal const int DefaultClientSendBufferSize = 16 * 1024;
        internal const int MaxControlFramePayloadLength = 123;
        internal static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromMinutes(2);

        // RFC 6455 requests WebSocket clients to let the server initiate the TCP close to avoid that client sockets
        // end up in TIME_WAIT-state
        //
        // After both sending and receiving a Close message, an endpoint considers the WebSocket connection closed and
        // MUST close the underlying TCP connection.  The server MUST close the underlying TCP connection immediately;
        // the client SHOULD wait for the server to close the connection but MAY close the connection at any time after
        // sending and receiving a Close message, e.g., if it has not received a TCP Close from the server in a
        // reasonable time period.
        internal const int ClientTcpCloseTimeout = 1000; // 1s

        private const int CloseStatusCodeAbort = 1006;
        private const int CloseStatusCodeFailedTLSHandshake = 1015;
        private const int InvalidCloseStatusCodesFrom = 0;
        private const int InvalidCloseStatusCodesTo = 999;
        private const string Separators = "()<>@,;:\\\"/[]?={} ";

        internal static readonly ArraySegment<byte> EmptyPayload = new ArraySegment<byte>(new byte[] { }, 0, 0);
        private static readonly Random KeyGenerator = new Random();

        internal static bool AreWebSocketsSupported
        {
            get
            {
                return ComNetOS.IsWin8orLater;
            }
        }

        internal static bool IsValidWebSocketKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }
            // TODO:
            // throw new NotImplementedException();
            return true;
        }

        internal static string GetSecWebSocketAcceptString(string secWebSocketKey)
        {
            string retVal;
            // SHA1 used only for hashing purposes, not for crypto. Check here for FIPS compat.
            using (SHA1 sha1 = SHA1.Create())
            {
                string acceptString = string.Concat(secWebSocketKey, WebSocketHelpers.SecWebSocketKeyGuid);
                byte[] toHash = Encoding.UTF8.GetBytes(acceptString);
                retVal = Convert.ToBase64String(sha1.ComputeHash(toHash));
            }
            return retVal;
        }

        internal static WebSocket CreateServerWebSocket(Stream opaqueStream, string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
        {
            return ManagedWebSocket.CreateFromConnectedStream(opaqueStream, isServer: true, subprotocol: subProtocol,
                keepAliveIntervalSeconds: (int)keepAliveInterval.TotalSeconds, receiveBufferSize: receiveBufferSize);
        }

        // return value here signifies if a Sec-WebSocket-Protocol header should be returned by the server.
        internal static bool ProcessWebSocketProtocolHeader(IEnumerable<string> clientSecWebSocketProtocols, string subProtocol)
        {
            if (clientSecWebSocketProtocols == null || !clientSecWebSocketProtocols.Any())
            {
                // client hasn't specified any Sec-WebSocket-Protocol header
                if (!string.IsNullOrEmpty(subProtocol))
                {
                    // If the server specified _anything_ this isn't valid.
                    throw new WebSocketException(WebSocketError.UnsupportedProtocol,
                        "The client did not specify a Sec-WebSocket-Protocol header. SubProtocol: " + subProtocol);
                }
                // Treat empty and null from the server as the same thing here, server should not send headers.
                return false;
            }

            // here, we know the client specified something and it's non-empty.

            if (string.IsNullOrEmpty(subProtocol))
            {
                // client specified some protocols, server specified 'null'. So server should send headers.
                return false;
            }

            // here, we know that the client has specified something, it's not empty
            // and the server has specified exactly one protocol

            // client specified protocols, serverOptions has exactly 1 non-empty entry. Check that
            // this exists in the list the client specified.
            foreach (var currentRequestProtocol in clientSecWebSocketProtocols)
            {
                if (string.Compare(subProtocol, currentRequestProtocol, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            throw new WebSocketException(WebSocketError.UnsupportedProtocol,
                $"Unsupported protocol: {subProtocol}; Client supported protocols: {string.Join(", ", clientSecWebSocketProtocols)}");
        }

        internal static void ValidateSubprotocol(string subProtocol)
        {
            if (string.IsNullOrEmpty(subProtocol))
            {
                return;
            }

            char[] chars = subProtocol.ToCharArray();
            string invalidChar = null;
            int i = 0;
            while (i < chars.Length)
            {
                char ch = chars[i];
                if (ch < 0x21 || ch > 0x7e)
                {
                    invalidChar = string.Format(CultureInfo.InvariantCulture, "[{0}]", (int)ch);
                    break;
                }

                if (!char.IsLetterOrDigit(ch) &&
                    Separators.IndexOf(ch) >= 0)
                {
                    invalidChar = ch.ToString();
                    break;
                }

                i++;
            }

            if (invalidChar != null)
            {
                throw new ArgumentException($"Invalid character '{invalidChar}' in the subProtocol '{subProtocol}'", nameof(subProtocol));
            }
        }

        internal static void ValidateOptions(string subProtocol, TimeSpan keepAliveInterval)
        {
            ValidateSubprotocol(subProtocol);

            // -1
            if (keepAliveInterval < Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(keepAliveInterval), keepAliveInterval,
                    "The value must be greater than or equal too 0 seconds, or -1 second to disable.");
            }
        }
    }
}
