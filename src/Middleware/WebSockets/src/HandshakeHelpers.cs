// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets
{
    internal static class HandshakeHelpers
    {
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
        public static void GenerateResponseHeaders(string key, string? subProtocol, IHeaderDictionary headers)
        {
            headers.Connection = HeaderNames.Upgrade;
            headers.Upgrade = Constants.Headers.UpgradeWebSocket;
            headers.SecWebSocketAccept = CreateResponseKey(key);
            if (!string.IsNullOrWhiteSpace(subProtocol))
            {
                headers.SecWebSocketProtocol = subProtocol;
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

            // requestKey is already verified to be small (24 bytes) by 'IsRequestKeyValid()' and everything is 1:1 mapping to UTF8 bytes
            // so this can be hardcoded to 60 bytes for the requestKey + static websocket string
            Span<byte> mergedBytes = stackalloc byte[60];
            Encoding.UTF8.GetBytes(requestKey, mergedBytes);
            EncodedWebSocketKey.CopyTo(mergedBytes[24..]);

            Span<byte> hashedBytes = stackalloc byte[20];
            var written = SHA1.HashData(mergedBytes, hashedBytes);
            if (written != 20)
            {
                throw new InvalidOperationException("Could not compute the hash for the 'Sec-WebSocket-Accept' header.");
            }

            return Convert.ToBase64String(hashedBytes);
        }

        // https://datatracker.ietf.org/doc/html/rfc7692#section-7.1
        public static bool ParseDeflateOptions(ReadOnlySpan<char> extension, WebSocketDeflateOptions options, [NotNullWhen(true)] out string? response)
        {
            bool hasServerMaxWindowBits = false;
            bool hasClientMaxWindowBits = false;
            response = null;
            var builder = new StringBuilder(WebSocketDeflateConstants.MaxExtensionLength);
            builder.Append(WebSocketDeflateConstants.Extension);

            while (true)
            {
                int end = extension.IndexOf(';');
                ReadOnlySpan<char> value = (end >= 0 ? extension[..end] : extension).Trim();

                if (value.Length > 0)
                {
                    if (value.SequenceEqual(WebSocketDeflateConstants.ClientNoContextTakeover))
                    {
                        options.ClientContextTakeover = false;
                        builder.Append("; ").Append(WebSocketDeflateConstants.ClientNoContextTakeover);
                    }
                    else if (value.SequenceEqual(WebSocketDeflateConstants.ServerNoContextTakeover))
                    {
                        // REVIEW: Do we want to reject it?
                        // Client requests no context takeover but options passed in specified context takeover, so reject the negotiate offer
                        if (options.ServerContextTakeover)
                        {
                            return false;
                        }
                    }
                    else if (value.StartsWith(WebSocketDeflateConstants.ClientMaxWindowBits))
                    {
                        var clientMaxWindowBits = ParseWindowBits(value, WebSocketDeflateConstants.ClientMaxWindowBits);
                        // 8 is a valid value according to the spec, but our zlib implementation does not support it
                        if (clientMaxWindowBits == 8)
                        {
                            return false;
                        }

                        // https://tools.ietf.org/html/rfc7692#section-7.1.2.2
                        // the server may either ignore this
                        // value or use this value to avoid allocating an unnecessarily big LZ77
                        // sliding window by including the "client_max_window_bits" extension
                        // parameter in the corresponding extension negotiation response to the
                        // offer with a value equal to or smaller than the received value.
                        options.ClientMaxWindowBits = Math.Min(clientMaxWindowBits ?? 15, options.ClientMaxWindowBits);

                        // If a received extension negotiation offer doesn't have the
                        // "client_max_window_bits" extension parameter, the corresponding
                        // extension negotiation response to the offer MUST NOT include the
                        // "client_max_window_bits" extension parameter.
                        builder.Append("; ").Append(WebSocketDeflateConstants.ClientMaxWindowBits).Append('=')
                            .Append(options.ClientMaxWindowBits.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (value.StartsWith(WebSocketDeflateConstants.ServerMaxWindowBits))
                    {
                        hasServerMaxWindowBits = true;
                        var serverMaxWindowBits = ParseWindowBits(value, WebSocketDeflateConstants.ServerMaxWindowBits);
                        // 8 is a valid value according to the spec, but our zlib implementation does not support it
                        if (serverMaxWindowBits == 8)
                        {
                            return false;
                        }

                        // https://tools.ietf.org/html/rfc7692#section-7.1.2.1
                        // A server accepts an extension negotiation offer with this parameter
                        // by including the "server_max_window_bits" extension parameter in the
                        // extension negotiation response to send back to the client with the
                        // same or smaller value as the offer.
                        options.ServerMaxWindowBits = Math.Min(serverMaxWindowBits ?? 15, options.ServerMaxWindowBits);
                    }

                    static int? ParseWindowBits(ReadOnlySpan<char> value, string propertyName)
                    {
                        var startIndex = value.IndexOf('=');

                        // parameters can be sent without a value by the client, we'll use the values set by the app developer or the default of 15
                        if (startIndex < 0)
                        {
                            return null;
                        }

                        if (!int.TryParse(value[(startIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int windowBits) ||
                            windowBits < 8 ||
                            windowBits > 15)
                        {
                            throw new WebSocketException(WebSocketError.HeaderError, $"invalid {propertyName} used: {value[(startIndex + 1)..].ToString()}");
                        }

                        return windowBits;
                    }
                }

                if (end < 0)
                {
                    break;
                }
                extension = extension[(end + 1)..];
            }

            if (!options.ServerContextTakeover)
            {
                builder.Append("; ").Append(WebSocketDeflateConstants.ServerNoContextTakeover);
            }

            if (hasServerMaxWindowBits || options.ServerMaxWindowBits != 15)
            {
                builder.Append("; ")
                    .Append(WebSocketDeflateConstants.ServerMaxWindowBits).Append('=')
                    .Append(options.ServerMaxWindowBits.ToString(CultureInfo.InvariantCulture));
            }

            // https://tools.ietf.org/html/rfc7692#section-7.1.2.2
            // If a received extension negotiation offer doesn't have the
            // "client_max_window_bits" extension parameter, the corresponding
            // extension negotiation response to the offer MUST NOT include the
            // "client_max_window_bits" extension parameter.
            //
            // Absence of this extension parameter in an extension negotiation
            // response indicates that the server can receive messages compressed
            // using an LZ77 sliding window of up to 32,768 bytes.
            if (!hasClientMaxWindowBits)
            {
                options.ClientMaxWindowBits = 15;
            }

            response = builder.ToString();

            return true;
        }
    }
}
