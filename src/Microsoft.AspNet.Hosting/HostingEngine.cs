// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine : IHostingEngine
    {
        private readonly IServiceCollection _applicationServiceCollection;
        private readonly IStartupLoader _startupLoader;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConfiguration _config;

        // Start/ApplicationServices block use methods
        private bool _useDisabled;

        private IServerLoader _serverLoader;
        private IApplicationBuilderFactory _builderFactory;
        private IApplicationBuilder _builder;
        private IServiceProvider _applicationServices;

        // Only one of these should be set
        private string _startupAssemblyName;
        private StartupMethods _startup;

        // Only one of these should be set
        private string _serverFactoryLocation;
        private IServerFactory _serverFactory;
        private IServerInformation _serverInstance;

        public HostingEngine(IServiceCollection appServices, IStartupLoader startupLoader, IConfiguration config, IHostingEnvironment hostingEnv, string appName)
        {
            _config = config ?? new Configuration();
            _applicationServiceCollection = appServices;
            _startupLoader = startupLoader;
            _startupAssemblyName = appName;
            _applicationLifetime = new ApplicationLifetime();
            _hostingEnvironment = hostingEnv;
        }

        public virtual IDisposable Start()
        {
            EnsureApplicationServices();
            EnsureBuilder();
            EnsureServer();

            var applicationDelegate = BuildApplicationDelegate();

            var _contextFactory = _applicationServices.GetRequiredService<IHttpContextFactory>();
            var _contextAccessor = _applicationServices.GetRequiredService<IHttpContextAccessor>();
            var server = _serverFactory.Start(_serverInstance,
                features =>
                {
                    var httpContext = _contextFactory.CreateHttpContext(features);
                    _contextAccessor.HttpContext = httpContext;
                    return applicationDelegate(httpContext);
                });

            return new Disposable(() =>
            {
                _applicationLifetime.NotifyStopping();
                server.Dispose();
                _applicationLifetime.NotifyStopped();
            });
        }

        private void EnsureApplicationServices()
        {
            _useDisabled = true;
            EnsureStartup();

            _applicationServiceCollection.AddInstance<IApplicationLifetime>(_applicationLifetime);

            _applicationServices = _startup.ConfigureServicesDelegate(_applicationServiceCollection);
        }

        private void EnsureStartup()
        {
            if (_startup != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            _startup = _startupLoader.Load(
                _startupAssemblyName,
                _hostingEnvironment.EnvironmentName,
                diagnosticMessages);

            if (_startup == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find a startup entry point for the web application.", (a, b) => a + "\r\n" + b),
                    _startupAssemblyName);
            }
        }

        private void EnsureBuilder()
        {
            if (_builderFactory == null)
            {
                _builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
            }

            _builder = _builderFactory.CreateBuilder();
            _builder.ApplicationServices = _applicationServices;
        }

        private void EnsureServer()
        {
            if (_serverFactory == null)
            {
                // Blow up if we don't have a server set at this point
                if (_serverFactoryLocation == null)
                {
                    throw new InvalidOperationException("UseStartup() is required for Start()");
                }

                _serverFactory = _applicationServices.GetRequiredService<IServerLoader>().LoadServerFactory(_serverFactoryLocation);
            }

            _serverInstance = _serverFactory.Initialize(_config);
            _builder.Server = _serverInstance;
        }

        private RequestDelegate BuildApplicationDelegate()
        {
            var startupFilters = _applicationServices.GetService<IEnumerable<IStartupFilter>>();
            var configure = _startup.ConfigureDelegate;
            foreach (var filter in startupFilters)
            {
                configure = filter.Configure(_builder, configure);
            }

            configure(_builder);

            return _builder.Build();
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                EnsureApplicationServices();
                return _applicationServices;
            }
        }

        private void CheckUseAllowed()
        {
            if (_useDisabled)
            {
                throw new InvalidOperationException("HostingEngine has already been started.");
            }
        }

        // Consider cutting
        public IHostingEngine UseEnvironment(string environment)
        {
            CheckUseAllowed();
            _hostingEnvironment.EnvironmentName = environment;
            return this;
        }

        public IHostingEngine UseServer(string assemblyName)
        {
            CheckUseAllowed();
            _serverFactoryLocation = assemblyName;
            return this;
        }

        public IHostingEngine UseServer(IServerFactory factory)
        {
            CheckUseAllowed();
            _serverFactory = factory;
            return this;
        }

        public IHostingEngine UseStartup(string startupAssemblyName)
        {
            CheckUseAllowed();
            _startupAssemblyName = startupAssemblyName;
            return this;
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp)
        {
            return UseStartup(configureApp, configureServices: null);
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, ConfigureServicesDelegate configureServices)
        {
            CheckUseAllowed();
            _startup = new StartupMethods(configureApp, configureServices);
            return this;
        }

        public IHostingEngine UseStartup(Action<IApplicationBuilder> configureApp, Action<IServiceCollection> configureServices)
        {
            CheckUseAllowed();
            _startup = new StartupMethods(configureApp, 
                services => {
                    if (configureServices != null)
                    {
                        configureServices(services);
                    }
                    return services.BuildServiceProvider();
                });
            return this;
        }

        private class Disposable : IDisposable
        {
            private Action _dispose;

            public Disposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, () => { }).Invoke();
            }
        }
    }
}