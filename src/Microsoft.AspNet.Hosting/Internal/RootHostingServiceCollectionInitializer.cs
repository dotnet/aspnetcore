// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting.Internal
{
    public class RootHostingServiceCollectionInitializer
    {
        private readonly IServiceProvider _fallbackServices;
        private readonly Action<IServiceCollection> _configureServices;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILoggerFactory _loggerFactory;

        public RootHostingServiceCollectionInitializer(IServiceProvider fallbackServices, Action<IServiceCollection> configureServices)
        {
            _fallbackServices = fallbackServices;
            _configureServices = configureServices;
            _hostingEnvironment = new HostingEnvironment();
            _loggerFactory = new LoggerFactory();
        }

        public IServiceCollection Build()
        {
            var services = new ServiceCollection();

            // Import from manifest
            var manifest = _fallbackServices.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => _fallbackServices.GetService(service));
            }

            services.AddInstance(_hostingEnvironment);
            services.AddInstance(this);
            services.AddInstance(_loggerFactory);

            services.AddTransient<IHostingFactory, HostingFactory>();
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
    }
}