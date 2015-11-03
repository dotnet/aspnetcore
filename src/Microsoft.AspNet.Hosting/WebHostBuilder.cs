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
using Microsoft.Extensions.Compilation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostBuilder
    {
        public const string OldApplicationKey = "app";
        public const string ApplicationKey = "Hosting:Application";

        public const string OldServerKey = "server";
        public const string ServerKey = "Hosting:Server";

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _config;

        private Action<IServiceCollection> _configureServices;

        // Only one of these should be set
        private StartupMethods _startup;
        private Type _startupType;
        private string _startupAssemblyName;
        private readonly bool _captureStartupErrors;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;
        private IServer _server;

        public WebHostBuilder()
            : this(config: new ConfigurationBuilder().Build())
        {
        }

        public WebHostBuilder(IConfiguration config)
            : this(config: config, captureStartupErrors: false)
        {
        }

        public WebHostBuilder(IConfiguration config, bool captureStartupErrors)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
            _config = config;
            _captureStartupErrors = captureStartupErrors;
        }

        private IServiceCollection BuildHostingServices()
        {
            var services = new ServiceCollection();
            services.AddInstance(_hostingEnvironment);
            services.AddInstance(_loggerFactory);

            services.AddTransient<IStartupLoader, StartupLoader>();

            services.AddTransient<IServerLoader, ServerLoader>();
            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNet");
            services.AddInstance<DiagnosticSource>(diagnosticSource);
            services.AddInstance<DiagnosticListener>(diagnosticSource);

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            if (_configureServices != null)
            {
                _configureServices(services);
            }

            if (PlatformServices.Default?.Application != null)
            {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.Application));
            }

            if (PlatformServices.Default?.Runtime != null)
            {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.Runtime));
            }

            if (PlatformServices.Default?.AssemblyLoadContextAccessor != null)
            {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.AssemblyLoadContextAccessor));
            }

            if (PlatformServices.Default?.AssemblyLoaderContainer != null)
            {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.AssemblyLoaderContainer));
            }

            if (PlatformServices.Default?.LibraryManager != null)
            {
                services.TryAdd(ServiceDescriptor.Instance(PlatformServices.Default.LibraryManager));
            }

            return services;
        }

        public IHostingEngine Build()
        {
            var hostingServices = BuildHostingServices();

            var hostingContainer = hostingServices.BuildServiceProvider();

            var appEnvironment = hostingContainer.GetRequiredService<IApplicationEnvironment>();
            var startupLoader = hostingContainer.GetRequiredService<IStartupLoader>();

            _hostingEnvironment.Initialize(appEnvironment.ApplicationBasePath, _config);
            var engine = new HostingEngine(hostingServices, startupLoader, _config, _captureStartupErrors);

            // Only one of these should be set, but they are used in priority
            engine.Server = _server;
            engine.ServerFactory = _serverFactory;
            engine.ServerFactoryLocation = _config[ServerKey] ?? _config[OldServerKey] ?? _serverFactoryLocation;

            // Only one of these should be set, but they are used in priority
            engine.Startup = _startup;
            engine.StartupType = _startupType;
            engine.StartupAssemblyName = _startupAssemblyName ?? _config[ApplicationKey] ?? _config[OldApplicationKey] ?? appEnvironment.ApplicationName;

            return engine;
        }

        public WebHostBuilder UseServices(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
            return this;
        }

        public WebHostBuilder UseEnvironment(string environment)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            _hostingEnvironment.EnvironmentName = environment;
            return this;
        }

        public WebHostBuilder UseServer(IServer server)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            _server = server;
            return this;
        }

        public WebHostBuilder UseServerFactory(string assemblyName)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            _serverFactoryLocation = assemblyName;
            return this;
        }

        public WebHostBuilder UseServerFactory(IServerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _serverFactory = factory;
            return this;
        }

        public WebHostBuilder UseStartup(string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }

            _startupAssemblyName = startupAssemblyName;
            return this;
        }

        public WebHostBuilder UseStartup(Type startupType)
        {
            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            _startupType = startupType;
            return this;
        }

        public WebHostBuilder UseStartup<TStartup>() where TStartup : class
        {
            return UseStartup(typeof(TStartup));
        }

        public WebHostBuilder UseStartup(Action<IApplicationBuilder> configureApp)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            return UseStartup(configureApp, configureServices: null);
        }

        public WebHostBuilder UseStartup(Action<IApplicationBuilder> configureApp, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            _startup = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public WebHostBuilder UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            if (configureApp == null)
            {
                throw new ArgumentNullException(nameof(configureApp));
            }

            _startup = new StartupMethods(configureApp,
                services =>
                {
                    if (configureServices != null)
                    {
                        configureServices(services);
                    }
                    return services.BuildServiceProvider();
                });
            return this;
        }
    }
}
