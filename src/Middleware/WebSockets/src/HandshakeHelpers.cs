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
            try
            {
                byte[] data = Convert.FromBase64String(value);
                return data.Length == 16;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string CreateResponseKey(string requestKey)
        {
            // "The value of this header field is constructed by concatenating /key/, defined above in step 4
            // in Section 4.2.2, with the string "258EAFA5- E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
            // this concatenated value to obtain a 20-byte value and base64-encoding"
            // https://tools.ietf.org/html/rfc6455#section-4.2.2

            if (requestKey == null)
            {
                throw new ArgumentNullException(nameof(requestKey));
            }

            using (var algorithm = SHA1.Create())
            {
                string merged = requestKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] mergedBytes = Encoding.UTF8.GetBytes(merged);
                byte[] hashedBytes = algorithm.ComputeHash(mergedBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
