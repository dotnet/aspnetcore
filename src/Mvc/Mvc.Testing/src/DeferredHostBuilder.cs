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
            // This will never be bull if the case where Build is being called
            return new DeferredHost((IHost)_hostFactory!(Array.Empty<string>()));
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

        private class DeferredHost : IHost
        {
            private IHost _host;
            public DeferredHost(IHost host)
            {
                _host = host;
            }

            public IServiceProvider Services => _host.Services;

            public void Dispose() => _host.Dispose();

            public Task StartAsync(CancellationToken cancellationToken = default)
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                // Wait on the existing host to start running and have this call wait on that. This avoids starting the actual host too early and
                // leaves the application in charge of calling start.

                // REVIEW: This will deadlock if the application creates the host but never calls start.
                _host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() => tcs.TrySetResult());

                return tcs.Task;
            }

            public Task StopAsync(CancellationToken cancellationToken = default) => _host.StopAsync();
        }
    }
}
