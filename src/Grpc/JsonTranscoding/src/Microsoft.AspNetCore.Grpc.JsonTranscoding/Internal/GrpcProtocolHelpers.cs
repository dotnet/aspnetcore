// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal static class GrpcProtocolHelpers
{
    public static byte[] ParseBinaryHeader(string base64)
    {
        string decodable;
        switch (base64.Length % 4)
        {
            case 0:
                // base64 has the required padding
                decodable = base64;
                break;
            case 2:
                // 2 chars padding
                decodable = base64 + "==";
                break;
            case 3:
                // 3 chars padding
                decodable = base64 + "=";
                break;
            default:
                // length%4 == 1 should be illegal
                throw new FormatException("Invalid base64 header value");
        }

        return Convert.FromBase64String(decodable);
    }
}
