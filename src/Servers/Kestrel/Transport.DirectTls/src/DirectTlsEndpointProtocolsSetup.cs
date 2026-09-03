// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

/// <summary>
/// Copies each <see cref="DirectTlsEndpoint"/>'s configured <see cref="ListenOptions.Protocols"/> onto the
/// endpoint's own <see cref="DirectTlsEndpointOptions.HttpProtocols"/>. Runs as a post-configure step so it
/// observes the final protocols after every <c>Listen</c> callback has executed. This lets
/// <see cref="DirectTlsTransportFactory"/> read the HTTP protocols (for the ALPN list) straight off the
/// endpoint at bind time, instead of taking a <see cref="KestrelServerOptions"/> dependency and matching the
/// endpoint back to its <see cref="ListenOptions"/>.
/// </summary>
internal sealed class DirectTlsEndpointProtocolsSetup : IPostConfigureOptions<KestrelServerOptions>
{
    public void PostConfigure(string? name, KestrelServerOptions options)
    {
        foreach (var listenOptions in options.GetListenOptions())
        {
            if (listenOptions.EndPoint is DirectTlsEndpoint directTlsEndpoint)
            {
                directTlsEndpoint.Options.HttpProtocols = listenOptions.Protocols;
            }
        }
    }
}
