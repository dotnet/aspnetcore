// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal class Host : IHost
    {
        private ILogger<Host> _logger;
        private IHostLifetime _hostLifetime;
        private ApplicationLifetime _applicationLifetime;
        private IEnumerable<IHostedService> _hostedServices;

        internal Host(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _applicationLifetime = Services.GetRequiredService<IApplicationLifetime>() as ApplicationLifetime;
            _logger = Services.GetRequiredService<ILogger<Host>>();
            _hostLifetime = Services.GetRequiredService<IHostLifetime>();
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.Starting();

            var delayStart = new TaskCompletionSource<object>();
            cancellationToken.Register(obj => ((TaskCompletionSource<object>)obj).TrySetCanceled(), delayStart);
            _hostLifetime.RegisterDelayStartCallback(obj => ((TaskCompletionSource<object>)obj).TrySetResult(null), delayStart);
            _hostLifetime.RegisterStopCallback(obj => (obj as IApplicationLifetime)?.StopApplication(), _applicationLifetime);

            await delayStart.Task;

            _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            foreach (var hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            // Fire IApplicationLifetime.Started
            _applicationLifetime?.NotifyStarted();

            _logger.Started();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.Stopping();
            
            // Trigger IApplicationLifetime.ApplicationStopping
            _applicationLifetime?.StopApplication();

            IList<Exception> exceptions = new List<Exception>();
            if (_hostedServices != null) // Started?
            {
                foreach (var hostedService in _hostedServices.Reverse())
                {
                    try
                    {
                        await hostedService.StopAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }

            await _hostLifetime.StopAsync(cancellationToken);

            // Fire IApplicationLifetime.Stopped
            _applicationLifetime?.NotifyStopped();

            if (exceptions.Count > 0)
            {
                var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                _logger.StoppedWithException(ex);
                throw ex;
            }

            _logger.Stopped();
        }

        public void Dispose()
        {
            (Services as IDisposable)?.Dispose();
        }
    }
}
