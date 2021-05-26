// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting
{
    // This exists solely to bootstrap the configuration
    internal class BootstrapHostBuilder : IHostBuilder
    {
        private readonly Configuration _configuration;
        private readonly WebHostEnvironment _environment;

        private readonly HostBuilderContext _hostContext;

        private readonly List<Action<IConfigurationBuilder>> _configureHostActions = new();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppActions = new();

        public BootstrapHostBuilder(Configuration configuration, WebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _environment = webHostEnvironment;

            _hostContext = new HostBuilderContext(Properties)
            {
                Configuration = configuration,
                HostingEnvironment = webHostEnvironment
            };
        }

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

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
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _configureHostActions.Add(configureDelegate ?? throw new ArgumentNullException(nameof(configureDelegate)));
            return this;
        }

        public string? GetSetting(string key)
        {
            return _configuration[key];
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via ConfigureLogging
            // during the initial config stage. It should be called again later on the ConfigureHostBuilder.
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            // This is not called by HostingHostBuilderExtensions.ConfigureDefaults currently, but that could change in the future.
            // If this does get called in the future, it should be called again at a later stage on the ConfigureHostBuilder.
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via UseDefaultServiceProvider
            // during the initial config stage. It should be called again later on the ConfigureHostBuilder.
            return this;
        }

        internal void RunConfigurationCallbacks()
        {
            foreach (var configureHostAction in _configureHostActions)
            {
                configureHostAction(_configuration);
            }

            // Configuration doesn't auto-update during the bootstrap phase to reduce I/O,
            // but we do need to update between host and app configuration so the right environment is used.
            _configuration.Update();
            _environment.ApplyConfigurationSettings(_configuration);

            foreach (var configureAppAction in _configureAppActions)
            {
                configureAppAction(_hostContext, _configuration);
            }

            _configuration.Update();
            _environment.ApplyConfigurationSettings(_configuration);
        }
    }
}
