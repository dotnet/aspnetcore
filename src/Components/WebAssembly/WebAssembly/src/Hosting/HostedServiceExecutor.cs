// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed partial class HostedServiceExecutor
{
    private readonly IEnumerable<IHostedService> _services;
    private readonly ILogger<HostedServiceExecutor> _logger;

    public HostedServiceExecutor(IEnumerable<IHostedService> services, ILogger<HostedServiceExecutor> logger)
    {
        _services = services;
        _logger = logger;
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
            var aggregateException = new AggregateException(exceptions);
            try
            {
                Log.ErrorStoppingHostedServices(_logger, aggregateException);
            }
            catch
            {
                // Ignore logging errors
            }
            throw aggregateException;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Error, "An error occurred stopping hosted services.", EventName = "ErrorStoppingHostedServices")]
        public static partial void ErrorStoppingHostedServices(ILogger logger, Exception exception);
    }
}
