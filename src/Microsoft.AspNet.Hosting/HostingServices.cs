// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
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
                // REVIEW: should this be Singleton instead?
                services.AddTransient(service, sp => fallbackProvider.GetService(service));
            }
            return services;
        }

        public static IServiceCollection Create(IConfiguration configuration = null)
        {
            return Create(CallContextServiceLocator.Locator.ServiceProvider);
        }

        public static IServiceCollection Create(IServiceProvider fallbackServices, IConfiguration configuration = null)
        {
            var services = Import(fallbackServices);
            services.Add(GetDefaultServices(configuration));
            services.AddSingleton<IServiceManifest>(sp => new HostingManifest(fallbackServices));
            return services;
        }

        // REVIEW: make this private?
        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration = null)
        {
            configuration = configuration ?? new Configuration();
            var describer = new ServiceDescriber(configuration);

            yield return describer.Transient<IHostingEngine, HostingEngine>();
            yield return describer.Transient<IServerManager, ServerManager>();

            yield return describer.Transient<IStartupManager, StartupManager>();
            yield return describer.Transient<IStartupLoaderProvider, StartupLoaderProvider>();

            yield return describer.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            yield return describer.Transient<IHttpContextFactory, HttpContextFactory>();

            yield return describer.Instance<IApplicationLifetime>(new ApplicationLifetime());

            // These three services as exported in the manifest
            yield return describer.Singleton<ITypeActivator, TypeActivator>();
            yield return describer.Singleton<IHostingEnvironment, HostingEnvironment>();
            // TODO: Do we expect this to be provide by the runtime eventually?
            yield return describer.Singleton<ILoggerFactory, LoggerFactory>();

            // TODO: Remove the below services and push the responsibility to frameworks to add

            yield return describer.Scoped(typeof(IContextAccessor<>), typeof(ContextAccessor<>));

            foreach (var service in OptionsServices.GetDefaultServices())
            {
                yield return service;
            }
        }

        private class HostingManifest : IServiceManifest
        {
            public HostingManifest(IServiceProvider fallback)
            {
                var manifest = fallback.GetRequiredService<IServiceManifest>();
                Services = new Type[] { typeof(ITypeActivator), typeof(IHostingEnvironment), typeof(ILoggerFactory) }
                    .Concat(manifest.Services).Distinct();
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}
