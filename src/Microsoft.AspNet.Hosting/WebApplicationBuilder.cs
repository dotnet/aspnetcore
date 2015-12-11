// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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

namespace Microsoft.AspNet.Hosting
{
    public class WebApplicationBuilder
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;

        private IConfiguration _config;
        private WebApplicationOptions _options;

        private Action<IServiceCollection> _configureServices;
        private string _environmentName;
        private string _webRoot;

        // Only one of these should be set
        private StartupMethods _startup;
        private Type _startupType;
        private string _startupAssemblyName;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;
        private IServer _server;

        public WebApplicationBuilder()
        {
            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
        }

        public WebApplicationBuilder UseConfiguration(IConfiguration configuration)
        {
            _config = configuration;
            return this;
        }

        public WebApplicationBuilder UseEnvironment(string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _environmentName = environment;
            return this;
        }

        public WebApplicationBuilder UseWebRoot(string webRoot)
        {
            if (webRoot == null)
            {
                throw new ArgumentNullException(nameof(webRoot));
            }
            _webRoot = webRoot;
            return this;
        }

        public WebApplicationBuilder UseServer(IServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            _server = server;
            return this;
        }

        public WebApplicationBuilder UseServerFactory(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            _serverFactoryLocation = assemblyName;
            return this;
        }

        public WebApplicationBuilder UseServerFactory(IServerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _serverFactory = factory;
            return this;
        }

        public WebApplicationBuilder UseStartup(string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }

            _startupAssemblyName = startupAssemblyName;
            return this;
        }

        public WebApplicationBuilder UseStartup(Type startupType)
        {
            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            _startupType = startupType;
            return this;
        }

        public WebApplicationBuilder UseStartup<TStartup>() where TStartup : class
        {
            return UseStartup(typeof(TStartup));
        }

        public WebApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
            return this;
        }

        public WebApplicationBuilder Configure(Action<IApplicationBuilder> configureApp)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            _startup = new StartupMethods(configureApp);
            return this;
        }

        public WebApplicationBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
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

            _config = _config ?? WebApplicationConfiguration.GetDefault();
            _options = new WebApplicationOptions(_config);

            // Initialize the hosting environment
            _options.WebRoot = _webRoot ?? _options.WebRoot;
            _hostingEnvironment.Initialize(appEnvironment.ApplicationBasePath, _options, _config);

            if (!string.IsNullOrEmpty(_environmentName))
            {
                _hostingEnvironment.EnvironmentName = _environmentName;
            }

            var application = new WebApplication(hostingServices, startupLoader, _options, _config);

            // Only one of these should be set, but they are used in priority
            application.Server = _server;
            application.ServerFactory = _serverFactory;
            application.ServerFactoryLocation = _options.ServerFactoryLocation ?? _serverFactoryLocation;

            // Only one of these should be set, but they are used in priority
            application.Startup = _startup;
            application.StartupType = _startupType;
            application.StartupAssemblyName = _startupAssemblyName ?? _options.Application;

            application.Initialize();

            return application;
        }

        private IServiceCollection BuildHostingServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_hostingEnvironment);
            services.AddSingleton(_loggerFactory);

            services.AddTransient<IStartupLoader, StartupLoader>();

            services.AddTransient<IServerLoader, ServerLoader>();
            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddLogging();

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
                    services.TryAddSingleton(defaultPlatformServices.Application);
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
    }
}
