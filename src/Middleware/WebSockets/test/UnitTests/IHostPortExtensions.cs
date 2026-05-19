// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

public static class IHostPortExtensions
{
    public static int GetPort(this IHost host)
    {
        return host.GetPorts().First();
    }

    public static IEnumerable<int> GetPorts(this IHost host)
    {
        return host.GetUris()
            .Select(u => u.Port);
    }

    public static IEnumerable<Uri> GetUris(this IHost host)
    {
        return host.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses
            .Select(a => new Uri(a));
    }
}
