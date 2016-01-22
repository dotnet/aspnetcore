// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.WebSockets.Protocol
{
    public static class HandshakeHelpers
    {
        /// <summary>
        /// Gets request headers needed process the handshake on the server.
        /// </summary>
        public static IEnumerable<string> NeededHeaders
        {
            get
            {
                return new[]
                {
                    Constants.Headers.Upgrade,
                    Constants.Headers.Connection,
                    Constants.Headers.SecWebSocketKey,
                    Constants.Headers.SecWebSocketVersion
                };
            }
        }

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
                if (string.Equals(Constants.Headers.Connection, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.ConnectionUpgrade, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validConnection = true;
                    }
                }
                else if (string.Equals(Constants.Headers.Upgrade, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.UpgradeWebSocket, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validUpgrade = true;
                    }
                }
                else if (string.Equals(Constants.Headers.SecWebSocketVersion, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Constants.Headers.SupportedVersion, pair.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        validVersion = true;
                    }
                }
                else if (string.Equals(Constants.Headers.SecWebSocketKey, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    validKey = IsRequestKeyValid(pair.Value);
                }
            }

            return validConnection && validUpgrade && validVersion && validKey;
        }

        public static IEnumerable<KeyValuePair<string, string>> GenerateResponseHeaders(string key, string subProtocol)
        {
            yield return new KeyValuePair<string, string>(Constants.Headers.Connection, Constants.Headers.ConnectionUpgrade);
            yield return new KeyValuePair<string, string>(Constants.Headers.Upgrade, Constants.Headers.UpgradeWebSocket);
            yield return new KeyValuePair<string, string>(Constants.Headers.SecWebSocketAccept, CreateResponseKey(key));
            if (!string.IsNullOrWhiteSpace(subProtocol))
            {
                yield return new KeyValuePair<string, string>(Constants.Headers.SecWebSocketProtocol, subProtocol);
            }
        }

        /// <summary>
        /// Validates the Sec-WebSocket-Key request header
        /// "The value of this header field MUST be a nonce consisting of a randomly selected 16-byte value that has been base64-encoded."
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

        public static bool IsResponseKeyValid(string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// "The value of this header field MUST be a nonce consisting of a randomly selected 16-byte value that has been base64-encoded."
        /// </summary>
        /// <returns></returns>
        public static string CreateRequestKey()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// "...the base64-encoded SHA-1 of the concatenation of the |Sec-WebSocket-Key| (as a string, not base64-decoded) with the string
        /// '258EAFA5-E914-47DA-95CA-C5AB0DC85B11'"
        /// </summary>
        /// <param name="requestKey"></param>
        /// <returns></returns>
        public static string CreateResponseKey(string requestKey)
        {
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