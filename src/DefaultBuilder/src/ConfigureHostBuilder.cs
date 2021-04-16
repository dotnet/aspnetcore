// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// A non-buildable <see cref="IHostBuilder"/> for <see cref="WebApplicationBuilder"/>.
    /// Use <see cref="WebApplicationBuilder.Build"/> to build the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    public sealed class ConfigureHostBuilder : IHostBuilder
    {
        private Action<IHostBuilder>? _operations;

        /// <inheritdoc />
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        internal Configuration Configuration => _configuration;

        private readonly IConfigurationBuilder _hostConfiguration = new ConfigurationBuilder();

        private readonly WebHostEnvironment _environment;
        private readonly Configuration _configuration;
        private readonly IServiceCollection _services;

        internal ConfigureHostBuilder(Configuration configuration, WebHostEnvironment environment, IServiceCollection services, string[]? args)
        {
            _configuration = configuration;
            _environment = environment;
            _services = services;

            this.ConfigureDefaults(args);
        }

        IHost IHostBuilder.Build()
        {
            throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _operations += b => b.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _operations += b => b.ConfigureContainer(configureDelegate);
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            // HACK: We need to evaluate the host configuration as they are changes so that we have an accurate view of the world
            configureDelegate(_hostConfiguration);

            var config = _hostConfiguration.Build();

            _environment.ApplicationName = config[HostDefaults.ApplicationKey] ?? _environment.ApplicationName;
            _environment.ContentRootPath = config[HostDefaults.ContentRootKey] ?? _environment.ContentRootPath;
            _environment.EnvironmentName = config[HostDefaults.EnvironmentKey] ?? _environment.EnvironmentName;
            _environment.ResolveFileProviders(config);
            Configuration.ChangeBasePath(_environment.ContentRootPath);

            _operations += b => b.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            // Run these immediately so that they are observable by the imperative code
            configureDelegate(new HostBuilderContext(Properties)
            {
                Configuration = Configuration,
                HostingEnvironment = _environment
            },
            _services);

            return this;
        }

        /// <inheritdoc />
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            _operations += b => b.UseServiceProviderFactory(factory);
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            _operations += b => b.UseServiceProviderFactory(factory);
            return this;
        }

        /// <inheritdoc />
        public void ExecuteActions(IHostBuilder hostBuilder)
        {
            _operations?.Invoke(hostBuilder);
        }
    }
}
