// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

public static class TransportSelector
{
    public static IHostBuilder GetHostBuilder(long? maxReadBufferSize = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseSockets(options =>
                {
                    options.MaxReadBufferSize = maxReadBufferSize;
                });
            });
    }
}
