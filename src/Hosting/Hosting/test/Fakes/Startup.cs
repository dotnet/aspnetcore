// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class Startup : StartupBase
    {
        public Startup()
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o => o.Configured = true);
        }

        public void ConfigureDevServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Dev";
            });
        }

        public void ConfigureRetailServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Retail";
            });
        }

        public static void ConfigureStaticServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Static";
            });
        }

        public static IServiceProvider ConfigureStaticProviderServices()
        {
            var services = new ServiceCollection().AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "StaticProvider";
            });
            return services.BuildServiceProvider();
        }

        public static IServiceProvider ConfigureFallbackProviderServices(IServiceProvider fallback)
        {
            return fallback;
        }

        public static IServiceProvider ConfigureNullServices()
        {
            return null;
        }

        public IServiceProvider ConfigureProviderServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Provider";
            });
            return services.BuildServiceProvider();
        }

        public IServiceProvider ConfigureProviderArgsServices()
        {
            var services = new ServiceCollection().AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "ProviderArgs";
            });
            return services.BuildServiceProvider();
        }

        public virtual void Configure(IApplicationBuilder builder)
        {
        }
    }
}