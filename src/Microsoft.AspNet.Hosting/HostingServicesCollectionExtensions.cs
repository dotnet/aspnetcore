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
        public static IServiceCollection AddLogging(this IServiceCollection services)
        {
            return services.AddLogging(config: null);
        }

        // REVIEW: Logging doesn't depend on DI, where should this live?
        public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration config)
        {
            var describe = new ServiceDescriber(config);
            services.TryAdd(describe.Singleton<ILoggerFactory, LoggerFactory>());
            services.TryAdd(describe.Singleton(typeof(ILogger<>), typeof(Logger<>)));
            return services;
        }

        public static IServiceCollection AddHosting(this IServiceCollection services)
        {
            return services.AddHosting(configuration: null);
        }

        public static IServiceCollection AddHosting(this IServiceCollection services, IConfiguration configuration)
        {
            var describer = new ServiceDescriber(configuration);

            services.TryAdd(describer.Transient<IHostingEngine, HostingEngine>());
            services.TryAdd(describer.Transient<IServerManager, ServerManager>());

            services.TryAdd(describer.Transient<IStartupManager, StartupManager>());
            services.TryAdd(describer.Transient<IStartupLoaderProvider, StartupLoaderProvider>());

            services.TryAdd(describer.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>());
            services.TryAdd(describer.Transient<IHttpContextFactory, HttpContextFactory>());

            services.TryAdd(describer.Instance<IApplicationLifetime>(new ApplicationLifetime()));

            services.AddTypeActivator(configuration);
            // TODO: Do we expect this to be provide by the runtime eventually?
            services.AddLogging(configuration);
            services.TryAdd(describer.Singleton<IHostingEnvironment, HostingEnvironment>());
            services.TryAdd(describer.Singleton<IHttpContextAccessor, HttpContextAccessor>());

            // REVIEW: don't try add because we pull out IEnumerable<IConfigureHostingEnvironment>?
            services.AddInstance<IConfigureHostingEnvironment>(new ConfigureHostingEnvironment(configuration));

            return services;
        }
    }
}