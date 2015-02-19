// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        private static IServiceCollection Import(IServiceProvider fallbackProvider, Action<IServiceCollection> configureHostServices)
        {
            var services = new ServiceCollection();
            if (configureHostServices != null)
            {
                configureHostServices(services);
            }
            var manifest = fallbackProvider.GetRequiredService<IServiceManifest>();
            foreach (var service in manifest.Services)
            {
                services.AddTransient(service, sp => fallbackProvider.GetService(service));
            }
            services.AddSingleton<IServiceManifest>(sp => new HostingManifest(services));
            return services;
        }

        public static IServiceCollection Create()
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configureHostServices: null, configuration: null);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices)
        {
            return Create(fallbackServices, configureHostServices: null, configuration: null);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, Action<IServiceCollection> configureHostServices)
        {
            return Create(fallbackServices, configureHostServices, configuration: null);
        }

        public static IServiceCollection Create(Action<IServiceCollection> configureHostServices, IConfiguration configuration)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configureHostServices, configuration);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, IConfiguration configuration)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configureHostServices: null, configuration: configuration);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, Action<IServiceCollection> configureHostServices, IConfiguration configuration)
        {
            var services = Import(fallbackServices, configureHostServices);
            services.AddHosting(configuration);
            return services;
        }

        // Manifest exposes the fallback manifest in addition to ITypeActivator, IHostingEnvironment, and ILoggerFactory
        private class HostingManifest : IServiceManifest
        {
            public HostingManifest(IServiceCollection hostServices)
            {
                Services = new Type[] { typeof(ITypeActivator), typeof(IHostingEnvironment), typeof(ILoggerFactory), typeof(IHttpContextAccessor) }
                    .Concat(hostServices.Select(s => s.ServiceType)).Distinct();
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}