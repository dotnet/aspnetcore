// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SpaProxy;

internal sealed class SpaProxyStartupFilter : IStartupFilter
{
    private readonly SpaProxyLaunchManager _spaProxyLaunchManager;
    private readonly IHostApplicationLifetime _hostLifetime;
    private readonly ILogger<SpaProxyStartupFilter> _logger;

    public SpaProxyStartupFilter(
        SpaProxyLaunchManager spaProxyLaunchManager,
        IHostApplicationLifetime hostLifetime,
        ILogger<SpaProxyStartupFilter> logger)
    {
        _spaProxyLaunchManager = spaProxyLaunchManager ?? throw new ArgumentNullException(nameof(spaProxyLaunchManager));
        _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        _ = StartIfNotRunning();
        return builder =>
        {
            builder.UseMiddleware<SpaProxyMiddleware>();
            next(builder);
        };

        async Task StartIfNotRunning()
        {
            try
            {
                if (!await _spaProxyLaunchManager.IsSpaProxyRunning(_hostLifetime.ApplicationStopping))
                {
                    _spaProxyLaunchManager.StartInBackground(_hostLifetime.ApplicationStopping);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was an error trying to launch the SPA proxy.");
            }
        }
    }
}
