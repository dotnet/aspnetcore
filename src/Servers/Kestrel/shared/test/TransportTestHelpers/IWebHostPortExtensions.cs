// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Hosting;

public static class IWebHostPortExtensions
{
    public static int GetPort(this IWebHost host)
    {
        return host.GetPorts().First();
    }

    public static IEnumerable<int> GetPorts(this IWebHost host)
    {
        return host.GetUris()
            .Select(u => u.Port);
    }

    public static IEnumerable<Uri> GetUris(this IWebHost host)
    {
        return host.ServerFeatures.Get<IServerAddressesFeature>().Addresses
            .Select(a => new Uri(a));
    }
}
