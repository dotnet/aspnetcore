// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;
namespace CodeGenerator;

public static class Http2Connection
{
    public static string GenerateFile()
    {
        return ReadOnlySpanStaticDataGenerator.GenerateFile(
            namespaceName: "Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2",
            className: "Http2Connection",
            allProperties: GetStrings());
    }

    private static IEnumerable<(string Name, string Value)> GetStrings()
    {
        yield return ("ClientPreface", "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");
        yield return ("Authority", ":authority");// PseudoHeaderNames.Authority);
        yield return ("Method", ":method");// PseudoHeaderNames.Method);
        yield return ("Path", ":path");// PseudoHeaderNames.Path);
        yield return ("Scheme", ":scheme");// PseudoHeaderNames.Scheme);
        yield return ("Status", ":status");// PseudoHeaderNames.Status);
        yield return ("Connection", "connection");
        yield return ("Te", "te");
        yield return ("Trailers", "trailers");
        yield return ("Connect", "CONNECT");
    }
}
