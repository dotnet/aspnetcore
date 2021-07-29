// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
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
        private readonly ConfigurationManager _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IServiceCollection _services;
        private readonly HostBuilderContext _context;

        private readonly List<Action<IHostBuilder>> _operations = new();

        internal ConfigureHostBuilder(
            ConfigurationManager configuration,
            IWebHostEnvironment environment,
            IServiceCollection services,
            IDictionary<object, object> properties)
        {
            _configuration = configuration;
            _environment = environment;
            _services = services;

            Properties = properties;

            _context = new HostBuilderContext(Properties)
            {
                Configuration = _configuration,
                HostingEnvironment = _environment
            };
        }

        /// <inheritdoc />
        public IDictionary<object, object> Properties { get; }

        IHost IHostBuilder.Build()
        {
            throw new NotSupportedException($"Call {nameof(WebApplicationBuilder)}.{nameof(WebApplicationBuilder.Build)}() instead.");
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            // Run these immediately so that they are observable by the imperative code
            configureDelegate(_context, _configuration);
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
            var beforeApplicationName = _configuration[HostDefaults.ApplicationKey];
            var beforeContentRoot = _configuration[HostDefaults.ContentRootKey];
            var beforeEnvironment = _configuration[HostDefaults.EnvironmentKey];

            // Run these immediately so that they are observable by the imperative code
            configureDelegate(_configuration);

            var afterApplicationName = _configuration[HostDefaults.ApplicationKey];
            var afterContentRoot = _configuration[HostDefaults.ContentRootKey];
            var afterEnvironment = _configuration[HostDefaults.EnvironmentKey];

            // Disallow changing any host settings this late in the cycle, the reasoning is that we've already loaded the default configuration
            // and done other things based on environment name, application name or content root.
            if (!string.Equals(beforeApplicationName, afterApplicationName, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("The application name changed.Changing the host configuration is not supported");
            }

            if (!string.Equals(beforeContentRoot, afterContentRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("The content root changed. Changing the host configuration is not supported");
            }

            if (!string.Equals(beforeEnvironment, afterEnvironment, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("The environment changed. Changing the host configuration is not supported");
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
