// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class HostingServicesExtensions
    {
        public static IServiceCollection AddHosting(this IServiceCollection services)
        {
            return services.AddHosting(configuration: null);
        }

        public static IServiceCollection AddHosting(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAdd(ServiceDescriptor.Transient<IHostingEngine, HostingEngine>());
            services.TryAdd(ServiceDescriptor.Transient<IServerLoader, ServerLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IStartupLoader, StartupLoader>());

            services.TryAdd(ServiceDescriptor.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(ServiceDescriptor.Transient<IHttpContextFactory, HttpContextFactory>());

            services.TryAdd(ServiceDescriptor.Instance<IApplicationLifetime>(new ApplicationLifetime()));

            services.AddTypeActivator();

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