// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed class HostedServiceExecutor
{
    private readonly IEnumerable<IHostedService> _services;

    public HostedServiceExecutor(IEnumerable<IHostedService> services)
    {
        _services = services;
    }

    public async Task StartAsync(CancellationToken token)
    {
        foreach (var service in _services)
        {
            await service.StartAsync(token);
        }
    }

    public async Task StopAsync(CancellationToken token)
    {
        List<Exception>? exceptions = null;

        foreach (var service in _services)
        {
            try
            {
                await service.StopAsync(token);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        // Throw an aggregate exception if there were any exceptions
        if (exceptions is not null)
        {
            throw new AggregateException(exceptions);
        }
    }
}
