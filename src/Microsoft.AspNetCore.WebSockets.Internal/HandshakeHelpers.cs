// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.WebSockets.Internal
{
    public static class HandshakeHelpers
    {
        // Verify Method, Upgrade, Connection, version,  key, etc..
        public static bool CheckSupportedWebSocketRequest(HttpRequest request)
        {
            bool validUpgrade = false, validConnection = false, validKey = false, validVersion = false;

            if (!string.Equals("GET", request.Method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var pair in request.Headers)
            {
                if (string.Equals(Constants.Headers.Connection, pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var value in pair.Value)
                    {
                        if (string.Equals(Constants.Headers.ConnectionUpgrade, value, StringComparison.OrdinalIgnoreCase))
                        {
                            validConnection = true;
                            break;
                        }
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
            return value.Length == 24;
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
