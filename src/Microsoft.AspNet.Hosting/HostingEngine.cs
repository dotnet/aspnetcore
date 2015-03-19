// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEngine
    {
        private const string EnvironmentKey = "ASPNET_ENV";

        private readonly IServiceProvider _fallbackServices;
        private readonly ApplicationLifetime _appLifetime;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly HostingEnvironment _hostingEnvironment;

        private IServerLoader _serverLoader;
        private IApplicationBuilderFactory _builderFactory;

        public HostingEngine() : this(fallbackServices: null) { }

        public HostingEngine(IServiceProvider fallbackServices)
        {
            _fallbackServices = fallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            _appLifetime = new ApplicationLifetime();
            _applicationEnvironment = _fallbackServices.GetRequiredService<IApplicationEnvironment>();
            _hostingEnvironment = new HostingEnvironment(_applicationEnvironment);
            _fallbackServices = new WrappingServiceProvider(_fallbackServices, _hostingEnvironment, _appLifetime);
        }

        public IDisposable Start(HostingContext context)
        {
            EnsureContextDefaults(context);
            EnsureApplicationServices(context);
            EnsureBuilder(context);
            EnsureServerFactory(context);
            InitalizeServerFactory(context);
            EnsureApplicationDelegate(context);

            var contextFactory = context.ApplicationServices.GetRequiredService<IHttpContextFactory>();
            var contextAccessor = context.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            var server = context.ServerFactory.Start(context.Server,
                features =>
                {
                    var httpContext = contextFactory.CreateHttpContext(features);
                    contextAccessor.HttpContext = httpContext;
                    return context.ApplicationDelegate(httpContext);
                });

            return new Disposable(() =>
            {
                _appLifetime.NotifyStopping();
                server.Dispose();
                _appLifetime.NotifyStopped();
            });
        }

        private void EnsureContextDefaults(HostingContext context)
        {
            if (context.ApplicationName == null)
            {
                context.ApplicationName = _applicationEnvironment.ApplicationName;
            }

            if (context.EnvironmentName == null)
            {
                context.EnvironmentName = context.Configuration?.Get(EnvironmentKey) ?? HostingEnvironment.DefaultEnvironmentName;
            }

            _hostingEnvironment.EnvironmentName = context.EnvironmentName;

            if (context.WebRootPath != null)
            {
                _hostingEnvironment.WebRootPath = context.WebRootPath;
            }
        }

        private void EnsureApplicationServices(HostingContext context)
        {
            if (context.ApplicationServices != null)
            {
                return;
            }

            EnsureStartupMethods(context);

            context.ApplicationServices = context.StartupMethods.ConfigureServicesDelegate(CreateHostingServices(context));
        }

        private void EnsureStartupMethods(HostingContext context)
        {
            if (context.StartupMethods != null)
            {
                return;
            }

            var diagnosticMessages = new List<string>();
            context.StartupMethods = ApplicationStartup.LoadStartupMethods(
                _fallbackServices,
                context.ApplicationName,
                context.EnvironmentName,
                diagnosticMessages);

            if (context.StartupMethods == null)
            {
                throw new ArgumentException(
                    diagnosticMessages.Aggregate("Failed to find an entry point for the web application.", (a, b) => a + "\r\n" + b),
                    nameof(context));
            }
        }

        private void EnsureBuilder(HostingContext context)
        {
            if (context.Builder != null)
            {
                return;
            }

            if (_builderFactory == null)
            {
                _builderFactory = context.ApplicationServices.GetRequiredService<IApplicationBuilderFactory>();
            }

            context.Builder = _builderFactory.CreateBuilder();
            context.Builder.ApplicationServices = context.ApplicationServices;
        }

        private void EnsureServerFactory(HostingContext context)
        {
            if (context.ServerFactory != null)
            {
                return;
            }

            if (_serverLoader == null)
            {
                _serverLoader = context.ApplicationServices.GetRequiredService<IServerLoader>();
            }

            context.ServerFactory = _serverLoader.LoadServerFactory(context.ServerFactoryLocation);
        }

        private void InitalizeServerFactory(HostingContext context)
        {
            if (context.Server == null)
            {
                context.Server = context.ServerFactory.Initialize(context.Configuration);
            }

            if (context.Builder.Server == null)
            {
                context.Builder.Server = context.Server;
            }
        }

        private IServiceCollection CreateHostingServices(HostingContext context)
        {
            var services = Import(_fallbackServices);

            services.TryAdd(ServiceDescriptor.Transient<IServerLoader, ServerLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(ServiceDescriptor.Transient<IHttpContextFactory, HttpContextFactory>());

            // TODO: Do we expect this to be provide by the runtime eventually?
            services.AddLogging();
            services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            // Apply user services
            services.Add(context.Services);

            // Jamming in app lifetime and hosting env since these must not be replaceable
            services.AddInstance<IApplicationLifetime>(_appLifetime);
            services.AddInstance<IHostingEnvironment>(_hostingEnvironment);

            // Conjure up a RequestServices
            services.AddTransient<IStartupFilter, AutoRequestServicesStartupFilter>();

            return services;
        }

        private void EnsureApplicationDelegate(HostingContext context)
        {
            if (context.ApplicationDelegate != null)
            {
                return;
            }

            // REVIEW: should we call EnsureApplicationServices?
            var startupFilters = context.ApplicationServices.GetService<IEnumerable<IStartupFilter>>();
            var configure = context.StartupMethods.ConfigureDelegate;
            foreach (var filter in startupFilters)
            {
                configure = filter.Configure(context.Builder, configure);
            }

            configure(context.Builder);

            context.ApplicationDelegate = context.Builder.Build();
        }

        private static IServiceCollection Import(IServiceProvider fallbackProvider)
        {
            var services = new ServiceCollection();
            var manifest = fallbackProvider.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => fallbackProvider.GetService(service));
            }

            return services;
        }

        private class WrappingServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider _sp;
            private readonly IHostingEnvironment _hostingEnvironment;
            private readonly IApplicationLifetime _applicationLifetime;

            public WrappingServiceProvider(IServiceProvider sp,
                                           IHostingEnvironment hostingEnvironment,
                                           IApplicationLifetime applicationLifetime)
            {
                _sp = sp;
                _hostingEnvironment = hostingEnvironment;
                _applicationLifetime = applicationLifetime;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IHostingEnvironment))
                {
                    return _hostingEnvironment;
                }

                if (serviceType == typeof(IApplicationLifetime))
                {
                    return _applicationLifetime;
                }

                return _sp.GetService(serviceType);
            }
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