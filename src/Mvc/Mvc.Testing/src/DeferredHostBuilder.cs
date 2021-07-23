// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Mvc.Testing
{
    // This host builder captures calls to the IHostBuilder then replays them in the call to ConfigureHostBuilder
    internal class DeferredHostBuilder : IHostBuilder
    {
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        private Action<IHostBuilder> _configure;
        private Func<string[], object>? _hostFactory;

        // This task represents a call to IHost.Start, we create it here preemptively in case the application
        // exits due to an exception or because it didn't wait for the shutdown signal
        private readonly TaskCompletionSource _hostStartTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DeferredHostBuilder()
        {
            _configure = b =>
            {
                // Copy the properties from this builder into the builder
                // that we're going to receive
                foreach (var pair in Properties)
                {
                    b.Properties[pair.Key] = pair.Value;
                }
            };
        }

        public IHost Build()
        {
            // This will never be null if the case where Build is being called
            var host = (IHost)_hostFactory!(Array.Empty<string>());

            // We can't return the host directly since we need to defer the call to StartAsync
            return new DeferredHost(host, _hostStartTcs);
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configure += b => b.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _configure += b => b.ConfigureContainer(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configure += b => b.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _configure += b => b.ConfigureServices(configureDelegate);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            _configure += b => b.UseServiceProviderFactory(factory);
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            _configure += b => b.UseServiceProviderFactory(factory);
            return this;
        }

        public void ConfigureHostBuilder(object hostBuilder)
        {
            _configure(((IHostBuilder)hostBuilder));
        }

        public void EntryPointCompleted(Exception? exception)
        {
            // If the entry point completed we'll set the tcs just in case the application doesn't call IHost.Start/StartAsync.
            if (exception is not null)
            {
                _hostStartTcs.TrySetException(exception);
            }
            else
            {
                _hostStartTcs.TrySetResult();
            }
        }

        public void SetHostFactory(Func<string[], object> hostFactory)
        {
            _hostFactory = hostFactory;
        }

        private class DeferredHost : IHost, IAsyncDisposable
        {
            private readonly IHost _host;
            private readonly TaskCompletionSource _hostStartedTcs;

            public DeferredHost(IHost host, TaskCompletionSource hostStartedTcs)
            {
                _host = host;
                _hostStartedTcs = hostStartedTcs;
            }

            public IServiceProvider Services => _host.Services;

            public void Dispose() => _host.Dispose();

            public async ValueTask DisposeAsync()
            {
                if (_host is IAsyncDisposable disposable)
                {
                    await disposable.DisposeAsync().ConfigureAwait(false);
                    return;
                }
                Dispose();
            }

            public async Task StartAsync(CancellationToken cancellationToken = default)
            {
                // Wait on the existing host to start running and have this call wait on that. This avoids starting the actual host too early and
                // leaves the application in charge of calling start.

                using var reg = cancellationToken.UnsafeRegister(_ => _hostStartedTcs.TrySetCanceled(), null);

                // REVIEW: This will deadlock if the application creates the host but never calls start. This is mitigated by the cancellationToken
                // but it's rarely a valid token for Start
                using var reg2 = _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.UnsafeRegister(_ => _hostStartedTcs.TrySetResult(), null);

                await _hostStartedTcs.Task.ConfigureAwait(false);
            }

            public Task StopAsync(CancellationToken cancellationToken = default) => _host.StopAsync(cancellationToken);
        }
    }
}
