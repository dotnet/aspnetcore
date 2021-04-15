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
    internal class ConfigurationHostBuilder : IHostBuilder
    {
        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();
        private readonly HostBuilderContext _context;
        private readonly Configuration _configuration;

        public ConfigurationHostBuilder(Configuration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _context = new HostBuilderContext(Properties)
            {
                Configuration = configuration,
                HostingEnvironment = webHostEnvironment
            };
        }

        public IHost Build()
        {
            // HostingHostBuilderExtensions.ConfigureDefaults should never call this.
            throw new InvalidOperationException();
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            configureDelegate(_context, _configuration);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            // This is not called by HostingHostBuilderExtensions.ConfigureDefaults currently, but that could change in the future.
            // If this does get called in the future, it should be called again at a later stage on the DeferredHostBuillder.
            return this;
        }

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            configureDelegate(_configuration);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via ConfigureLogging
            // during the initial config stage. It should be called again later on the DeferredHostBuilder.
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            // This is not called by HostingHostBuilderExtensions.ConfigureDefaults currently, but that chould change in the future.
            // If this does get called in the future, it should be called again at a later stage on the DeferredHostBuillder.
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via UseDefaultServiceProvider
            // during the initial config stage. It should be called again later on the DeferredHostBuilder.
            return this;
        }
    }
}
