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
        private readonly ConfigurationManager _configuration;
        private readonly IServiceCollection _services;
        private readonly List<Action<IConfigurationBuilder>> _configureHostActions = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppActions = new();
        private readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServicesActions = new();

        private readonly List<Action<IHostBuilder>> _remainingOperations = new();

        public BootstrapHostBuilder(
            ConfigurationManager configuration,
            IServiceCollection services,
            IDictionary<object, object> properties)
        {
            _configuration = configuration;
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

        public void RunDefaultCallbacks(HostBuilder innerBuilder)
        {
            foreach (var configureHostAction in _configureHostActions)
            {
                configureHostAction(_configuration);
            }

            // This is the hosting environment based on configuration we've seen so far.
            var hostingEnvironment = new HostingEnvironment()
            {
                ApplicationName = _configuration[HostDefaults.ApplicationKey],
                EnvironmentName = _configuration[HostDefaults.EnvironmentKey] ?? Environments.Production,
                ContentRootPath = HostingEnvironment.ResolveContentRootPath(_configuration[HostDefaults.ContentRootKey], AppContext.BaseDirectory),
            };

            hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);

            var hostContext = new HostBuilderContext(Properties)
            {
                Configuration = _configuration,
                HostingEnvironment = hostingEnvironment,
            };

            _configuration.SetBasePath(hostingEnvironment.ContentRootPath);

            foreach (var configureAppAction in _configureAppActions)
            {
                configureAppAction(hostContext, _configuration);
            }

            foreach (var configureServicesAction in _configureServicesActions)
            {
                configureServicesAction(hostContext, _services);
            }

            foreach (var callback in _remainingOperations)
            {
                callback(innerBuilder);
            }
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
