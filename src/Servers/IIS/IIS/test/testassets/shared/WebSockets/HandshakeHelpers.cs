// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

internal static class HandshakeHelpers
{
    public static IEnumerable<KeyValuePair<string, string>> GenerateResponseHeaders(string key)
    {
        yield return new KeyValuePair<string, string>(Constants.Headers.Connection, Constants.Headers.Upgrade);
        yield return new KeyValuePair<string, string>(Constants.Headers.Upgrade, Constants.Headers.UpgradeWebSocket);
        yield return new KeyValuePair<string, string>(Constants.Headers.SecWebSocketAccept, CreateResponseKey(key));
    }

    public static string CreateResponseKey(string requestKey)
    {
        // "The value of this header field is constructed by concatenating /key/, defined above in step 4
        // in Section 4.2.2, with the string "258EAFA5- E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
        // this concatenated value to obtain a 20-byte value and base64-encoding"
        // https://tools.ietf.org/html/rfc6455#section-4.2.2

        ArgumentNullException.ThrowIfNull(requestKey);

        string merged = requestKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        byte[] mergedBytes = Encoding.UTF8.GetBytes(merged);
        byte[] hashedBytes = SHA1.HashData(mergedBytes);
        return Convert.ToBase64String(hashedBytes);
    }
}
