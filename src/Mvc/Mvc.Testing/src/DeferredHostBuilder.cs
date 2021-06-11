using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return new DeferredHost(host);
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

        public void SetHostFactory(Func<string[], object> hostFactory)
        {
            _hostFactory = hostFactory;
        }

        private class DeferredHost : IHost, IAsyncDisposable
        {
            private readonly IHost _host;

            public DeferredHost(IHost host)
            {
                _host = host;
            }

            public IServiceProvider Services => _host.Services;

            public void Dispose() => _host.Dispose();

            public ValueTask DisposeAsync()
            {
                if (_host is IAsyncDisposable disposable)
                {
                    return disposable.DisposeAsync();
                }
                Dispose();
                return default;
            }

            public Task StartAsync(CancellationToken cancellationToken = default)
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                // Wait on the existing host to start running and have this call wait on that. This avoids starting the actual host too early and
                // leaves the application in charge of calling start.

                using var reg = cancellationToken.UnsafeRegister(_ => tcs.TrySetCanceled(), null);

                // REVIEW: This will deadlock if the application creates the host but never calls start. This is mitigated by the cancellationToken
                // but it's rarely a valid token for Start
                _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.UnsafeRegister(_ => tcs.TrySetResult(), null);

                return tcs.Task;
            }

            public Task StopAsync(CancellationToken cancellationToken = default) => _host.StopAsync(cancellationToken);
        }
    }
}
