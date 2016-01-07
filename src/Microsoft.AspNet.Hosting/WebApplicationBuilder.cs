// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Hosting
{
    public class WebApplicationBuilder : IWebApplicationBuilder
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;

        private IConfiguration _config;
        private WebApplicationOptions _options;

        private Action<IServiceCollection> _configureServices;

        // Only one of these should be set
        private StartupMethods _startup;
        private Type _startupType;

        // Only one of these should be set
        private IServerFactory _serverFactory;

        private Dictionary<string, string> _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public WebApplicationBuilder()
        {
            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
        }

        public IWebApplicationBuilder UseSetting(string key, string value)
        {
            _settings[key] = value;
            return this;
        }

        public IWebApplicationBuilder UseConfiguration(IConfiguration configuration)
        {
            _config = configuration;
            return this;
        }

        public IWebApplicationBuilder UseServer(IServerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _serverFactory = factory;
            return this;
        }

        public IWebApplicationBuilder UseStartup(Type startupType)
        {
            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            _startupType = startupType;
            return this;
        }

        public IWebApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
            return this;
        }

        public IWebApplicationBuilder Configure(Action<IApplicationBuilder> configureApp)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            _startup = new StartupMethods(configureApp);
            return this;
        }

        public IWebApplicationBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
        {
            configureLogging(_loggerFactory);
            return this;
        }

        public IWebApplication Build()
        {
            var hostingServices = BuildHostingServices();

            var hostingContainer = hostingServices.BuildServiceProvider();

            var appEnvironment = hostingContainer.GetRequiredService<IApplicationEnvironment>();
            var startupLoader = hostingContainer.GetRequiredService<IStartupLoader>();

            // Initialize the hosting environment
            _hostingEnvironment.Initialize(appEnvironment.ApplicationBasePath, _options, _config);

            var application = new WebApplication(hostingServices, startupLoader, _options, _config);

            // Only one of these should be set, but they are used in priority
            application.ServerFactory = _serverFactory;
            application.ServerFactoryLocation = _options.ServerFactoryLocation;

            // Only one of these should be set, but they are used in priority
            application.Startup = _startup;
            application.StartupType = _startupType;
            application.StartupAssemblyName = _options.Application;

            application.Initialize();

            return application;
        }

        private IServiceCollection BuildHostingServices()
        {
            // Apply the configuration settings
            var configuration = _config ?? WebApplicationConfiguration.GetDefault();

            var mergedConfiguration = new ConfigurationBuilder()
                                .Add(new IncludedConfigurationProvider(configuration))
                                .AddInMemoryCollection(_settings)
                                .Build();

            _config = mergedConfiguration;
            _options = new WebApplicationOptions(_config);

            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_loggerFactory);

            services.AddTransient<IStartupLoader, StartupLoader>();

            services.AddTransient<IServerLoader, ServerLoader>();
            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddLogging();
            services.AddOptions();

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNet");
            services.AddSingleton<DiagnosticSource>(diagnosticSource);
            services.AddSingleton<DiagnosticListener>(diagnosticSource);

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            var defaultPlatformServices = PlatformServices.Default;

            if (defaultPlatformServices != null)
            {
                if (defaultPlatformServices.Application != null)
                {
                    var appEnv = defaultPlatformServices.Application;
                    if (!string.IsNullOrEmpty(_options.ApplicationBasePath))
                    {
                        appEnv = new WrappedApplicationEnvironment(_options.ApplicationBasePath, appEnv);
                    }

                    services.TryAddSingleton(appEnv);
                }

                if (defaultPlatformServices.Runtime != null)
                {
                    services.TryAddSingleton(defaultPlatformServices.Runtime);
                }
            }

            if (_configureServices != null)
            {
                _configureServices(services);
            }

            return services;
        }

        private class WrappedApplicationEnvironment : IApplicationEnvironment
        {
            public WrappedApplicationEnvironment(string applicationBasePath, IApplicationEnvironment env)
            {
                ApplicationBasePath = applicationBasePath;
                ApplicationName = env.ApplicationName;
                ApplicationVersion = env.ApplicationVersion;
                RuntimeFramework = env.RuntimeFramework;
            }

            public string ApplicationBasePath { get; }

            public string ApplicationName { get; }

            public string ApplicationVersion { get; }

            public FrameworkName RuntimeFramework { get; }
        }
    }
}
