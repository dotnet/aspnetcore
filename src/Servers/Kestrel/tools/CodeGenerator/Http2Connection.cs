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
        yield return ("ClientPreface", "PRI * HTTP/2.0\\r\\n\\r\\nSM\\r\\n\\r\\n");
        yield return ("Authority", HeaderNames.Authority);
        yield return ("Method", HeaderNames.Method);
        yield return ("Path", HeaderNames.Path);
        yield return ("Scheme", HeaderNames.Scheme);
        yield return ("Status", HeaderNames.Status);
        yield return ("Connection", "connection");
        yield return ("Te", "te");
        yield return ("Trailers", "trailers");
        yield return ("Connect", "CONNECT");
        yield return ("Protocol", ":protocol");
    }
}
