// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class HttpProtocolsFeature
{
    public HttpProtocolsFeature(HttpProtocols httpProtocols)
    {
        HttpProtocols = httpProtocols;
    }

    public HttpProtocols HttpProtocols { get; }
}
