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
            throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            configureDelegate(_context, _configuration);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => throw new NotImplementedException();

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            configureDelegate(_configuration);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via ConfigureLogging.
            // Apparently, this just doesn't work yet. :(
            //throw new NotImplementedException();
            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull => throw new NotImplementedException();

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            // HostingHostBuilderExtensions.ConfigureDefaults calls this via UseDefaultServiceProvider.
            // Apparently, this just doesn't work yet. :(
            //throw new NotImplementedException();
            return this;
        }
    }
}
