// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
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

        public static IServiceCollection Create(IConfiguration configuration = null)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider, configuration);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, IConfiguration configuration = null)
        {
            configuration = configuration ?? new Configuration();
            var services = Import(fallbackServices);
            services.AddHosting(configuration);
            services.AddSingleton<IServiceManifest>(sp => new HostingManifest(fallbackServices));
            return services;
        }

        // Manifest exposes the fallback manifest in addition to ITypeActivator, IHostingEnvironment, and ILoggerFactory
        private class HostingManifest : IServiceManifest
        {
            public HostingManifest(IServiceProvider fallback)
            {
                var manifest = fallback.GetRequiredService<IServiceManifest>();
                Services = new Type[] { typeof(ITypeActivator), typeof(IHostingEnvironment), typeof(ILoggerFactory), typeof(IHttpContextAccessor) }
                    .Concat(manifest.Services).Distinct();
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}