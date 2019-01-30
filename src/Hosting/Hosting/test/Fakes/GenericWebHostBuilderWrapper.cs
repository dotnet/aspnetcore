// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    public class GenericWebHostBuilderWrapper : IWebHostBuilder, ISupportsStartup, ISupportsUseDefaultServiceProvider
    {
        private readonly GenericWebHostBuilder _builder;
        private readonly HostBuilder _hostBuilder;

        internal GenericWebHostBuilderWrapper(HostBuilder hostBuilder)
        {
            _builder = new GenericWebHostBuilder(hostBuilder);
            _hostBuilder = hostBuilder;
        }

        // This is the only one that doesn't pass through
        public IWebHost Build()
        {
            return new GenericWebHost(_hostBuilder.Build());
        }

        public IWebHostBuilder Configure(Action<IApplicationBuilder> configure)
        {
            _builder.Configure(configure);
            return this;
        }

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            _builder.ConfigureServices(configureServices);
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _builder.ConfigureServices(configureServices);
            return this;
        }

        public string GetSetting(string key)
        {
            return _builder.GetSetting(key);
        }

        public IWebHostBuilder UseDefaultServiceProvider(Action<WebHostBuilderContext, ServiceProviderOptions> configure)
        {
            _builder.UseDefaultServiceProvider(configure);
            return this;
        }

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }

        public IWebHostBuilder UseStartup(Type startupType)
        {
            _builder.UseStartup(startupType);
            return this;
        }
    }
}
