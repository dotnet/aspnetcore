// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets
{
    internal static class HandshakeHelpers
    {
        /// <summary>
        /// Gets request headers needed process the handshake on the server.
        /// </summary>
        public static readonly IEnumerable<string> NeededHeaders = new[]
        {
            HeaderNames.Upgrade,
            HeaderNames.Connection,
            HeaderNames.SecWebSocketKey,
            HeaderNames.SecWebSocketVersion
        };

        // "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
        private static ReadOnlySpan<byte> EncodedWebSocketKey => new byte[]
        {
            (byte)'2', (byte)'5', (byte)'8', (byte)'E', (byte)'A', (byte)'F', (byte)'A', (byte)'5', (byte)'-',
            (byte)'E', (byte)'9', (byte)'1', (byte)'4', (byte)'-', (byte)'4', (byte)'7', (byte)'D', (byte)'A',
            (byte)'-', (byte)'9', (byte)'5', (byte)'C', (byte)'A', (byte)'-', (byte)'C', (byte)'5', (byte)'A',
            (byte)'B', (byte)'0', (byte)'D', (byte)'C', (byte)'8', (byte)'5', (byte)'B', (byte)'1', (byte)'1'
        };

        // Verify Method, Upgrade, Connection, version,  key, etc..
        public static bool CheckSupportedWebSocketRequest(string method, IEnumerable<KeyValuePair<string, string>> headers)
        {
            bool validUpgrade = false, validConnection = false, validKey = false, validVersion = false;

            if (!string.Equals("GET", method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var pair in headers)
            {
                if (string.Equals(HeaderNames.Connection, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.ConnectionUpgrade, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validConnection = true;
                    }
                }
                else if (string.Equals(HeaderNames.Upgrade, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.UpgradeWebSocket, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validUpgrade = true;
                    }
                }
                else if (string.Equals(HeaderNames.SecWebSocketVersion, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.SupportedVersion, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validVersion = true;
                    }
                }
                else if (string.Equals(HeaderNames.SecWebSocketKey, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    validKey = IsRequestKeyValid(pair.Value);
                }
            }

            return validConnection && validUpgrade && validVersion && validKey;
        }

        public static void GenerateResponseHeaders(string key, string subProtocol, IHeaderDictionary headers)
        {
            headers[HeaderNames.Connection] = Constants.Headers.ConnectionUpgrade;
            headers[HeaderNames.Upgrade] = Constants.Headers.UpgradeWebSocket;
            headers[HeaderNames.SecWebSocketAccept] = CreateResponseKey(key);
            if (!string.IsNullOrWhiteSpace(subProtocol))
            {
                headers[HeaderNames.SecWebSocketProtocol] = subProtocol;
            }
        }

        /// <summary>
        /// Validates the Sec-WebSocket-Key request header
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsRequestKeyValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            Span<byte> temp = stackalloc byte[16];
            var success = Convert.TryFromBase64String(value, temp, out var written);
            return success && written == 16;
        }

        public static string CreateResponseKey(string requestKey)
        {
            // "The value of this header field is constructed by concatenating /key/, defined above in step 4
            // in Section 4.2.2, with the string "258EAFA5-E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
            // this concatenated value to obtain a 20-byte value and base64-encoding"
            // https://tools.ietf.org/html/rfc6455#section-4.2.2

            using (var algorithm = SHA1.Create())
            {
                // requestKey is already verified to be small (24 bytes) by 'IsRequestKeyValid()' and everything is 1:1 mapping to UTF8 bytes
                // so this can be hardcoded to 60 bytes for the requestKey + static websocket string
                Span<byte> mergedBytes = stackalloc byte[60];
                Encoding.UTF8.GetBytes(requestKey, mergedBytes);
                EncodedWebSocketKey.CopyTo(mergedBytes.Slice(24));

                Span<byte> hashedBytes = stackalloc byte[20];
                var success = algorithm.TryComputeHash(mergedBytes, hashedBytes, out var written);
                if (!success || written != 20)
                {
                    throw new InvalidOperationException("Could not compute the hash for the 'Sec-WebSocket-Accept' header.");
                }

                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
