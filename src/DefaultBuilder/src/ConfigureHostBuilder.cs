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
        private readonly List<Action<IHostBuilder>> _operations = new();

        /// <inheritdoc />
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        private readonly WebHostEnvironment _environment;
        private readonly Configuration _configuration;
        private readonly IServiceCollection _services;

        private readonly HostBuilderContext _context;

        internal ConfigureHostBuilder(Configuration configuration, WebHostEnvironment environment, IServiceCollection services)
        {
            _configuration = configuration;
            _environment = environment;
            _services = services;

            _context = new HostBuilderContext(Properties)
            {
                Configuration = _configuration,
                HostingEnvironment = _environment
            };
        }

        internal bool ConfigurationEnabled { get; set; }

        IHost IHostBuilder.Build()
        {
            throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            if (ConfigurationEnabled)
            {
                // Run these immediately so that they are observable by the imperative code
                configureDelegate(_context, _configuration);
                _environment.ApplyConfigurationSettings(_configuration);
            }

            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            if (configureDelegate is null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }

            _operations.Add(b => b.ConfigureContainer(configureDelegate));
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            if (ConfigurationEnabled)
            {
                // Run these immediately so that they are observable by the imperative code
                configureDelegate(_configuration);
                _environment.ApplyConfigurationSettings(_configuration);
            }

            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            // Run these immediately so that they are observable by the imperative code
            configureDelegate(_context, _services);

            return this;
        }

        /// <inheritdoc />
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _operations.Add(b => b.UseServiceProviderFactory(factory));
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _operations.Add(b => b.UseServiceProviderFactory(factory));
            return this;
        }

        internal void RunDeferredCallbacks(IHostBuilder hostBuilder)
        {
            foreach (var operation in _operations)
            {
                operation(hostBuilder);
            }
        }
    }
}
