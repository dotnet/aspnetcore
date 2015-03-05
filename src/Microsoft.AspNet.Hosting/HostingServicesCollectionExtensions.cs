// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Logging;

namespace Microsoft.Framework.DependencyInjection
{
    public static class HostingServicesExtensions
    {
        // REVIEW: Logging doesn't depend on DI, where should this live?
        public static IServiceCollection AddLogging(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            return services;
        }

        public static IServiceCollection AddHosting(this IServiceCollection services, IConfiguration configuration = null)
        {
            services.TryAdd(ServiceDescriptor.Transient<IHostingEngine, HostingEngine>());
            services.TryAdd(ServiceDescriptor.Transient<IServerLoader, ServerLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IStartupLoader, StartupLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(ServiceDescriptor.Transient<IHttpContextFactory, HttpContextFactory>());

            services.TryAdd(ServiceDescriptor.Instance<IApplicationLifetime>(new ApplicationLifetime()));

            services.AddTypeActivator(configuration);
            // TODO: Do we expect this to be provide by the runtime eventually?
            services.AddLogging();
            services.TryAdd(ServiceDescriptor.Singleton<IHostingEnvironment, HostingEnvironment>());
            services.TryAdd(ServiceDescriptor.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            // REVIEW: don't try add because we pull out IEnumerable<IConfigureHostingEnvironment>?
            services.AddInstance<IConfigureHostingEnvironment>(new ConfigureHostingEnvironment(configuration));

            return services;
        }
    }
}