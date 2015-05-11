// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class HostingEngine : IHostingEngine
    {
        private readonly IServiceCollection _applicationServiceCollection;
        private readonly IStartupLoader _startupLoader;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly IConfiguration _config;

        private IServiceProvider _applicationServices;

        // Only one of these should be set
        internal string StartupAssemblyName { get; set; }
        internal StartupMethods Startup { get; set; }
        internal Type StartupType { get; set; }

        // Only one of these should be set
        internal IServerFactory ServerFactory { get; set; }
        internal string ServerFactoryLocation { get; set; }
        private IServerInformation _serverInstance;

        public HostingEngine(
            [NotNull] IServiceCollection appServices, 
            [NotNull] IStartupLoader startupLoader, 
            [NotNull] IConfiguration config)
        {
            _config = config;
            _applicationServiceCollection = appServices;
            _startupLoader = startupLoader;
            _applicationLifetime = new ApplicationLifetime();
        }

        public IServiceProvider ApplicationServices
        {
            get
            {
                EnsureApplicationServices();
                return _applicationServices;
            }
        }

        public virtual IDisposable Start()
        {
            EnsureApplicationServices();

            var application = BuildApplication();

            var logger = _applicationServices.GetRequiredService<ILogger<HostingEngine>>();
            var contextFactory = _applicationServices.GetRequiredService<IHttpContextFactory>();
            var contextAccessor = _applicationServices.GetRequiredService<IHttpContextAccessor>();
            var server = ServerFactory.Start(_serverInstance,
                async features =>
                {
                    var httpContext = contextFactory.CreateHttpContext(features);
                    var requestIdentifier = GetRequestIdentifier(httpContext);

                    using (logger.BeginScope("Request Id: {RequestId}", requestIdentifier))
                    {
                        contextAccessor.HttpContext = httpContext;
                        await application(httpContext);
                    }
                });

            _applicationLifetime.NotifyStarted();

            return new Disposable(() =>
            {
                _applicationLifetime.NotifyStopping();
                server.Dispose();
                _applicationLifetime.NotifyStopped();
            });
        }

        private void EnsureApplicationServices()
        {
            if (_applicationServices == null)
            {
                EnsureStartup();
                _applicationServiceCollection.AddInstance<IApplicationLifetime>(_applicationLifetime);
                _applicationServices = Startup.ConfigureServicesDelegate(_applicationServiceCollection);
            }
        }

        private void EnsureStartup()
        {
            if (Startup != null)
            {
                return;
            }

            if (StartupType == null)
            {
                var diagnosticTypeMessages = new List<string>();
                StartupType = _startupLoader.FindStartupType(StartupAssemblyName, diagnosticTypeMessages);
                if (StartupType == null)
                {
                    throw new ArgumentException(
                        diagnosticTypeMessages.Aggregate("Failed to find a startup type for the web application.", (a, b) => a + "\r\n" + b),
                        StartupAssemblyName);
                }
            }

            var diagnosticMessages = new List<string>();
            Startup = _startupLoader.LoadMethods(StartupType, diagnosticMessages);
            if (Startup == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find a startup entry point for the web application.", (a, b) => a + "\r\n" + b),
                    StartupAssemblyName);
            }
        }

        private RequestDelegate BuildApplication()
        {
            if (ServerFactory == null)
            {
                // Blow up if we don't have a server set at this point
                if (ServerFactoryLocation == null)
                {
                    throw new InvalidOperationException("IHostingBuilder.UseServer() is required for " + nameof(Start) + "()");
                }

                ServerFactory = _applicationServices.GetRequiredService<IServerLoader>().LoadServerFactory(ServerFactoryLocation);
            }

            _serverInstance = ServerFactory.Initialize(_config);
            var builderFactory = _applicationServices.GetRequiredService<IApplicationBuilderFactory>();
            var builder = builderFactory.CreateBuilder(_serverInstance);
            builder.ApplicationServices = _applicationServices;

            var startupFilters = _applicationServices.GetService<IEnumerable<IStartupFilter>>();
            var configure = Startup.ConfigureDelegate;
            foreach (var filter in startupFilters)
            {
                configure = filter.Configure(builder, configure);
            }

            configure(builder);

            return builder.Build();
        }

        private Guid GetRequestIdentifier(HttpContext httpContext)
        {
            var requestIdentifierFeature = httpContext.GetFeature<IRequestIdentifierFeature>();
            if (requestIdentifierFeature == null)
            {
                requestIdentifierFeature = new DefaultRequestIdentifierFeature();
                httpContext.SetFeature(requestIdentifierFeature);
            }

            return requestIdentifierFeature.TraceIdentifier;
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
