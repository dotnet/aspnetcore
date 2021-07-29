// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting
{
    // This exists solely to bootstrap the configuration
    internal class BootstrapHostBuilder : IHostBuilder
    {
        private readonly ConfigurationManager _configuration;
        private readonly WebHostEnvironment _environment;
        private readonly IServiceCollection _services;
        private readonly HostBuilderContext _hostContext;

        private readonly List<Action<IConfigurationBuilder>> _configureHostActions = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new();

        private readonly List<Action<IHostBuilder>> _remainingOperations = new();

        public BootstrapHostBuilder(
            ConfigurationManager configuration,
            WebHostEnvironment webHostEnvironment,
            IServiceCollection services,
            IDictionary<object, object> properties)
        {
            _configuration = configuration;
            _environment = webHostEnvironment;
            _services = services;

            Properties = properties;

            _hostContext = new HostBuilderContext(Properties)
            {
                Configuration = configuration,
                HostingEnvironment = webHostEnvironment
            };
        }

        public IDictionary<object, object> Properties { get; }

        public IHost Build()
        {
            // HostingHostBuilderExtensions.ConfigureDefaults should never call this.
            throw new InvalidOperationException();
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            // This is not called by HostingHostBuilderExtensions.ConfigureDefaults currently, but that could change in the future.
            // If this does get called in the future, it should be called again at a later stage on the ConfigureHostBuilder.
            if (configureDelegate is null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

            _remainingOperations.Add(hostBuilder => hostBuilder.ConfigureContainer<TContainerBuilder>(configureDelegate));
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via ConfigureLogging
            _configureServicesActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            // This is not called by HostingHostBuilderExtensions.ConfigureDefaults currently, but that could change in the future.
            // If this does get called in the future, it should be called again at a later stage on the ConfigureHostBuilder.
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _remainingOperations.Add(hostBuilder => hostBuilder.UseServiceProviderFactory<TContainerBuilder>(factory));
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via UseDefaultServiceProvider
            // during the initial config stage. It should be called again later on the ConfigureHostBuilder.
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _remainingOperations.Add(hostBuilder => hostBuilder.UseServiceProviderFactory<TContainerBuilder>(factory));
            return this;
        }

        public void RunDefaultCallbacks(HostBuilder innerBuilder)
        {
            foreach (var configureHostAction in _configureHostActions)
            {
                configureHostAction(_configuration);
            }

            // Configuration doesn't auto-update during the bootstrap phase to reduce I/O,
            // but we do need to update between host and app configuration so the right environment is used.
            _environment.ApplyConfigurationSettings(_configuration);

            foreach (var configureAppAction in _configureAppActions)
            {
                configureAppAction(_hostContext, _configuration);
            }

            _environment.ApplyConfigurationSettings(_configuration);

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(_hostContext, _services);
            }

            foreach (var callback in _remainingOperations)
            {
                callback(innerBuilder);
            }
        }
    }
}
