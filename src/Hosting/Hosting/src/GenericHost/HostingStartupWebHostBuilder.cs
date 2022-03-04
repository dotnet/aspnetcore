// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    // We use this type to capture calls to the IWebHostBuilder so the we can properly order calls to 
    // to GenericHostWebHostBuilder.
    internal class HostingStartupWebHostBuilder : IWebHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
    {
        private readonly GenericWebHostBuilder _builder;
        private Action<WebHostBuilderContext, IConfigurationBuilder> _configureConfiguration;
        private Action<WebHostBuilderContext, IServiceCollection> _configureServices;

        public HostingStartupWebHostBuilder(GenericWebHostBuilder builder)
        {
            _builder = builder;
        }

        public IWebHost Build()
        {
            throw new NotSupportedException($"Building this implementation of {nameof(IWebHostBuilder)} is not supported.");
        }

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureConfiguration += configureDelegate;
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((context, services) => configureServices(services));
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _configureServices += configureServices;
            return this;
        }

        public string GetSetting(string key) => _builder.GetSetting(key);

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }

        public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            _configureServices?.Invoke(context, services);
        }

        public void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
        {
            _configureConfiguration?.Invoke(context, builder);
        }

        public IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
        {
            return _builder.UseDefaultServiceProvider(configure);
        }

        public IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure)
        {
            return _builder.Configure(configure);
        }

        public IWebHostBuilder UseStartup(Type startupType)
        {
            return _builder.UseStartup(startupType);
        }
    }
}
