// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting
{
    // This exists solely to bootstrap the configuration
    internal class BootstrapHostBuilder : IHostBuilder
    {
        private readonly IServiceCollection _services;
        private readonly List<Action<IConfigurationBuilder>> _configureHostActions = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new();

        private readonly List<Action<IHostBuilder>> _remainingOperations = new();

        public BootstrapHostBuilder(IServiceCollection services, IDictionary<object, object> properties)
        {
            _services = services;

            Properties = properties;
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

        public (HostBuilderContext, ConfigurationManager) RunDefaultCallbacks(HostBuilder innerBuilder)
        {
            var hostConfiguration = new ConfigurationManager();

            foreach (var configureHostAction in _configureHostActions)
            {
                configureHostAction(hostConfiguration);
            }

            // This is the hosting environment based on configuration we've seen so far.
            var hostingEnvironment = new HostingEnvironment()
            {
                ApplicationName = hostConfiguration[HostDefaults.ApplicationKey],
                EnvironmentName = hostConfiguration[HostDefaults.EnvironmentKey] ?? Environments.Production,
                ContentRootPath = HostingEnvironment.ResolveContentRootPath(hostConfiguration[HostDefaults.ContentRootKey], AppContext.BaseDirectory),
            };

            hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);

            var hostContext = new HostBuilderContext(Properties)
            {
                Configuration = hostConfiguration,
                HostingEnvironment = hostingEnvironment,
            };

            // Split the host configuration and app configuration so that the
            // subsequent callback don't get a chance to modify the host configuration.
            var configuration = new ConfigurationManager();
            configuration.SetBasePath(hostingEnvironment.ContentRootPath);

            // Chain the host configuration and app configuration together.
            configuration.AddConfiguration(hostConfiguration, shouldDisposeConfiguration: true);

            // ConfigureAppConfiguration sees the hosting configuration only, this is important
            // so that code running during ConfigureAppConfiguration cannot see mutations to
            // host configuration while running (e.g. the environment, content root, nor application name won't change).
            foreach (var configureAppAction in _configureAppActions)
            {
                configureAppAction(hostContext, configuration);
            }

            // Update the host context, everything from here seems the final
            // app configuration
            hostContext.Configuration = configuration;

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(hostContext, _services);
            }

            foreach (var callback in _remainingOperations)
            {
                callback(innerBuilder);
            }

            return (hostContext, configuration);
        }

        private class HostingEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = default!;
            public string ApplicationName { get; set; } = default!;
            public string ContentRootPath { get; set; } = default!;
            public IFileProvider ContentRootFileProvider { get; set; } = default!;

            public static string ResolveContentRootPath(string contentRootPath, string basePath)
            {
                if (string.IsNullOrEmpty(contentRootPath))
                {
                    return basePath;
                }
                if (Path.IsPathRooted(contentRootPath))
                {
                    return contentRootPath;
                }
                return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
            }
        }
    }
}
