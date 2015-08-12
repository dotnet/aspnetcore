// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Hosting
{
    public class WebHostBuilder
    {
        public const string OldEnvironmentKey = "ASPNET_ENV";
        public const string EnvironmentKey = "Hosting:Environment";

        public const string OldApplicationKey = "app";
        public const string ApplicationKey = "Hosting:Application";

        public const string OldServerKey = "server";
        public const string ServerKey = "Hosting:Server";

        private readonly IServiceProvider _services;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _config;

        private Action<IServiceCollection> _configureServices;

        // Only one of these should be set
        private StartupMethods _startup;
        private Type _startupType;
        private string _startupAssemblyName;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;

        public WebHostBuilder([NotNull] IServiceProvider services)
            : this(services, config: new ConfigurationBuilder().Build())
        {
        }

        public WebHostBuilder([NotNull] IServiceProvider services, [NotNull] IConfiguration config)
        {
            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
            _services = services;
            _config = config;
        }

        private IServiceCollection BuildHostingServices()
        {
            var services = new ServiceCollection();

            // Import from manifest
            var manifest = _services.GetService<IRuntimeServices>();
            if (manifest != null)
            {
                foreach (var service in manifest.Services)
                {
                    services.AddTransient(service, sp => _services.GetService(service));
                }
            }

            services.AddInstance(_hostingEnvironment);
            services.AddInstance(_loggerFactory);

            services.AddTransient<IStartupLoader, StartupLoader>();

            services.AddTransient<IServerLoader, ServerLoader>();
            services.AddTransient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            services.AddTransient<IHttpContextFactory, HttpContextFactory>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddLogging();

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            if (_configureServices != null)
            {
                _configureServices(services);
            }

            return services;
        }

        public IHostingEngine Build()
        {
            var hostingServices = BuildHostingServices();

            var hostingContainer = hostingServices.BuildServiceProvider();

            var appEnvironment = hostingContainer.GetRequiredService<IApplicationEnvironment>();
            var startupLoader = hostingContainer.GetRequiredService<IStartupLoader>();

            _hostingEnvironment.Initialize(appEnvironment.ApplicationBasePath, _config?[EnvironmentKey] ?? _config?[OldEnvironmentKey]);

            var engine = new HostingEngine(hostingServices, startupLoader, _config);

            // Only one of these should be set, but they are used in priority
            engine.ServerFactory = _serverFactory;
            engine.ServerFactoryLocation = _config.Get(ServerKey) ?? _config.Get(OldServerKey) ?? _serverFactoryLocation;

            // Only one of these should be set, but they are used in priority
            engine.Startup = _startup;
            engine.StartupType = _startupType;
            engine.StartupAssemblyName = _startupAssemblyName ?? _config.Get(ApplicationKey) ?? _config.Get(OldApplicationKey) ?? appEnvironment.ApplicationName;

            return engine;
        }

        public WebHostBuilder UseServices(Action<IServiceCollection> configureServices)
        {
            _configureServices = configureServices;
            return this;
        }

        public WebHostBuilder UseEnvironment([NotNull] string environment)
        {
            _hostingEnvironment.EnvironmentName = environment;
            return this;
        }

        public WebHostBuilder UseServer([NotNull] string assemblyName)
        {
            _serverFactoryLocation = assemblyName;
            return this;
        }

        public WebHostBuilder UseServer(IServerFactory factory)
        {
            _serverFactory = factory;
            return this;
        }

        public WebHostBuilder UseStartup([NotNull] string startupAssemblyName)
        {
            if (startupAssemblyName == null)
            {
                throw new ArgumentNullException(nameof(startupAssemblyName));
            }
            _startupAssemblyName = startupAssemblyName;
            return this;
        }

        public WebHostBuilder UseStartup([NotNull] Type startupType)
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

        public WebHostBuilder UseStartup([NotNull] Action<IApplicationBuilder> configureApp)
        {
            return UseStartup(configureApp, configureServices: null);
        }

        public WebHostBuilder UseStartup([NotNull] Action<IApplicationBuilder> configureApp, Func<IServiceCollection, IServiceProvider> configureServices)
        {
            _startup = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public WebHostBuilder UseStartup([NotNull] Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
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
