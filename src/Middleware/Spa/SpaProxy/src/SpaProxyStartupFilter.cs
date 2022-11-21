// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SpaProxy;

internal sealed class SpaProxyStartupFilter : IStartupFilter
{
    private readonly SpaProxyLaunchManager _spaProxyLaunchManager;
    private readonly IHostApplicationLifetime _hostLifetime;

    public SpaProxyStartupFilter(
        SpaProxyLaunchManager spaProxyLaunchManager,
        IHostApplicationLifetime hostLifetime)
    {
        _spaProxyLaunchManager = spaProxyLaunchManager ?? throw new ArgumentNullException(nameof(spaProxyLaunchManager));
        _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        Task.Run(async () =>
        {
            if (!await _spaProxyLaunchManager.IsSpaProxyRunning(new CancellationToken()))
            {
                _spaProxyLaunchManager.StartInBackground(_hostLifetime.ApplicationStopping);
            }
        });
        return builder =>
        {
            builder.UseMiddleware<SpaProxyMiddleware>();
            next(builder);
        };
    }
}
