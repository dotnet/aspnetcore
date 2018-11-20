// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostedServiceExecutor
    {
        private readonly IEnumerable<IHostedService> _services;
        private readonly ILogger<HostedServiceExecutor> _logger;

        public HostedServiceExecutor(ILogger<HostedServiceExecutor> logger, IEnumerable<IHostedService> services)
        {
            _logger = logger;
            _services = services;
        }

        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                await ExecuteAsync(service => service.StartAsync(token));
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.HostedServiceStartException, "An error occurred starting the application", ex);
            }
        }

        public async Task StopAsync(CancellationToken token)
        {
            try
            {
                await ExecuteAsync(service => service.StopAsync(token));
            }
            catch (Exception ex)
            {
                _logger.ApplicationError(LoggerEventIds.HostedServiceStopException, "An error occurred stopping the application", ex);
            }
        }

        private async Task ExecuteAsync(Func<IHostedService, Task> callback)
        {
            List<Exception> exceptions = null;

            foreach (var service in _services)
            {
                try
                {
                    await callback(service);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            // Throw an aggregate exception if there were any exceptions
            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
