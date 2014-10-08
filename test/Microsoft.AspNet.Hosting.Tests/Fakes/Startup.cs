// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class Startup
    {
        public Startup()
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureOptions<FakeOptions>(o => o.Configured = true);
        }

        public void ConfigureServicesDev(IServiceCollection services)
        {
            services.ConfigureOptions<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Dev";
            });
        }

        public void ConfigureServicesRetail(IServiceCollection services)
        {
            services.ConfigureOptions<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Retail";
            });
        }

        public static void ConfigureServicesStatic(IServiceCollection services)
        {
            services.ConfigureOptions<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Static";
            });
        }

        public void Configure(IApplicationBuilder builder)
        {
        }
    }
}