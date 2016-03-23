// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Startup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// A builder for <see cref="IWebHost"/>
    /// </summary>
    public class WebHostBuilder : IWebHostBuilder
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;
        private readonly List<Action<IServiceCollection>> _configureServicesDelegates;

        private IConfiguration _config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        private WebHostOptions _options;

        // Only one of these should be set
        private StartupMethods _startup;
        private Type _startupType;

        // Only one of these should be set
        private IServerFactory _serverFactory;

        public WebHostBuilder()
        {
            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
            _configureServicesDelegates = new List<Action<IServiceCollection>>();
        }

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        public string GetSetting(string key)
        {
            return _config[key];
        }

        /// <summary>
        /// Specify the <see cref="IServerFactory"/> to be used by the web host.
        /// </summary>
        /// <param name="factory">The <see cref="IServerFactory"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseServer(IServerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _serverFactory = factory;
            return this;
        }

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <param name="startupType">The <see cref="Type"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder UseStartup(Type startupType)
        {
            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            _startupType = startupType;
            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            _configureServicesDelegates.Add(configureServices);
            return this;
        }

        /// <summary>
        /// Specify the startup method to be used to configure the web application.
        /// </summary>
        /// <param name="configureApp">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder Configure(Action<IApplicationBuilder> configureApp)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            _startup = new StartupMethods(configureApp);
            return this;
        }

        /// <summary>
        /// Configure the provided <see cref="ILoggerFactory"/> which will be available as a hosting service.
        /// </summary>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public IWebHostBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
        {
            configureLogging(_loggerFactory);
            return this;
        }

        /// <summary>
        /// Builds the required services and an <see cref="IWebHost"/> which hosts a web application.
        /// </summary>
        public IWebHost Build()
        {
            var hostingServices = BuildHostingServices();
            var hostingContainer = hostingServices.BuildServiceProvider();

            var appEnvironment = hostingContainer.GetRequiredService<IApplicationEnvironment>();
            var startupLoader = hostingContainer.GetRequiredService<IStartupLoader>();

            var contentRootPath = ResolveContentRootPath(_options.ContentRootPath, appEnvironment.ApplicationBasePath);
            var applicationName = ResolveApplicationName() ?? appEnvironment.ApplicationName;

            // Initialize the hosting environment
            _hostingEnvironment.Initialize(applicationName, contentRootPath, _options);

            var host = new WebHost(hostingServices, startupLoader, _options, _config);

            // Only one of these should be set, but they are used in priority
            host.ServerFactory = _serverFactory;
            host.ServerFactoryLocation = _options.ServerFactoryLocation;

            // Only one of these should be set, but they are used in priority
            host.Startup = _startup;
            host.StartupType = _startupType;
            host.StartupAssemblyName = _options.Application;

            host.Initialize();

            return host;
        }

        private IServiceCollection BuildHostingServices()
        {
            _options = new WebHostOptions(_config);

            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_loggerFactory);

            services.AddTransient<IStartupLoader, StartupLoader>();

            services.AddTransient<IServerLoader, ServerLoader>();
            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddLogging();
            services.AddOptions();

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddSingleton<DiagnosticListener>(diagnosticSource);

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            // Ensure object pooling is available everywhere.
            services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            var defaultPlatformServices = PlatformServices.Default;
            if (defaultPlatformServices.Application != null)
            {
                services.TryAddSingleton(defaultPlatformServices.Application);
            }
            if (defaultPlatformServices.Runtime != null)
            {
                services.TryAddSingleton(defaultPlatformServices.Runtime);
            }

            foreach (var configureServices in _configureServicesDelegates)
            {
                configureServices(services);
            }

            return services;
        }

        private string ResolveContentRootPath(string contentRootPath, string basePath)
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

        private string ResolveApplicationName()
        {
            if (_startup != null)
            {
                return _startup.ConfigureDelegate.Target.GetType().GetTypeInfo().Assembly.GetName().Name;
            }
            if (_startupType != null)
            {
                return _startupType.GetTypeInfo().Assembly.GetName().Name;
            }
            if (!string.IsNullOrEmpty(_options.Application))
            {
                return _options.Application;
            }
            return null;
        }
    }
}
