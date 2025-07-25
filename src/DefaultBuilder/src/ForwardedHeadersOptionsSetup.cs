// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace Microsoft.AspNetCore;

internal sealed class ForwardedHeadersOptionsSetup : IConfigureOptions<ForwardedHeadersOptions>
{
    private readonly IConfiguration _configuration;

    public ForwardedHeadersOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(ForwardedHeadersOptions options)
    {
        if (!string.Equals("true", _configuration["ForwardedHeaders_Enabled"], StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var forwardedHeaders = _configuration["ForwardedHeaders_Headers"];
        if (string.IsNullOrEmpty(forwardedHeaders))
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        }
        else
        {
            var headers = ForwardedHeaders.None;
            foreach (var headerName in forwardedHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<ForwardedHeaders>(headerName, true, out var headerValue))
                {
                    headers |= headerValue;
                }
            }
            options.ForwardedHeaders = headers;
        }

        // Only loopback proxies are allowed by default. Clear that restriction because forwarders are
        // being enabled by explicit configuration.
#pragma warning disable ASPDEPR005 // KnownNetworks is obsolete
        options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005 // KnownNetworks is obsolete
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        var knownNetworks = _configuration["ForwardedHeaders_KnownIPNetworks"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        foreach (var network in knownNetworks)
        {
            if (IPNetwork.TryParse(network, var ipNetwork))
            {
                options.KnownIPNetworks.Add(ipNetwork);
            }
        }

        var knownProxies = _configuration["ForwardedHeaders_KnownProxies"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var ipAddress))
            {
                options.KnownProxies.Add(ipAddress);
            }
        }
    }
}
