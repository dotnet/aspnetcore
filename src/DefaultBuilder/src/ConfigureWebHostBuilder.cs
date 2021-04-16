// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// A non-buildable <see cref="IWebHostBuilder"/> for <see cref="WebApplicationBuilder"/>.
    /// Use <see cref="WebApplicationBuilder.Build"/> to build the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    public sealed class ConfigureWebHostBuilder : IWebHostBuilder
    {
        private Action<IWebHostBuilder>? _operations;

        private readonly WebHostEnvironment _environment;
        private readonly Configuration _configuration;
        private readonly Dictionary<string, string?> _settings = new Dictionary<string, string?>();
        private readonly IServiceCollection _services;

        internal ConfigureWebHostBuilder(Configuration configuration, WebHostEnvironment environment, IServiceCollection services)
        {
            _configuration = configuration;
            _environment = environment;
            _services = services;
        }

        IWebHost IWebHostBuilder.Build()
        {
            throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
        }

        /// <inheritdoc />
        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _operations += b => b.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        /// <inheritdoc />
        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            configureServices(new WebHostBuilderContext
            {
                Configuration = _configuration,
                HostingEnvironment = _environment
            },
            _services);
            return this;
        }

        /// <inheritdoc />
        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            return ConfigureServices((WebHostBuilderContext context, IServiceCollection services) => configureServices(services));
        }

        /// <inheritdoc />
        public string? GetSetting(string key)
        {
            _settings.TryGetValue(key, out var value);
            return value;
        }

        /// <inheritdoc />
        public IWebHostBuilder UseSetting(string key, string? value)
        {
            _settings[key] = value;
            _operations += b => b.UseSetting(key, value);

            // All preoperties on IWebHostEnvironment are non-nullable.
            if (value is null)
            {
                return this;
            }

            if (key == WebHostDefaults.ApplicationKey)
            {
                _environment.ApplicationName = value;
            }
            else if (key == WebHostDefaults.ContentRootKey)
            {
                _environment.ContentRootPath = value;
                _environment.ResolveFileProviders(_configuration);

                _configuration.ChangeBasePath(value);
            }
            else if (key == WebHostDefaults.EnvironmentKey)
            {
                _environment.EnvironmentName = value;
            }
            else if (key == WebHostDefaults.WebRootKey)
            {
                _environment.WebRootPath = value;
                _environment.ResolveFileProviders(_configuration);
            }

            return this;
        }

        /// <inheritdoc />
        public void ExecuteActions(IWebHostBuilder webHostBuilder)
        {
            _operations?.Invoke(webHostBuilder);
        }
    }
}
