// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    internal class HostedServiceExecutor
    {
        private readonly IEnumerable<IHostedService> _services;
        private readonly ILogger<HostedServiceExecutor> _logger;

        public HostedServiceExecutor(ILogger<HostedServiceExecutor> logger, IEnumerable<IHostedService> services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken token)
        {
            return ExecuteAsync(service => service.StartAsync(token));
        }

        public Task StopAsync(CancellationToken token)
        {
            return ExecuteAsync(service => service.StopAsync(token), throwOnFirstFailure: false);
        }

        private async Task ExecuteAsync(Func<IHostedService, Task> callback, bool throwOnFirstFailure = true)
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
                    if (throwOnFirstFailure)
                    {
                        throw;
                    }

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
