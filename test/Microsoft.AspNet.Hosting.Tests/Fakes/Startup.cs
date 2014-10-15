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
            services.Configure<FakeOptions>(o => o.Configured = true);
        }

        public void ConfigureDevServices(IServiceCollection services)
        {
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Dev";
            });
        }

        public void ConfigureRetailServices(IServiceCollection services)
        {
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Retail";
            });
        }

        public static void ConfigureStaticServices(IServiceCollection services)
        {
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "Static";
            });
        }

        public virtual void Configure(IApplicationBuilder builder)
        {
        }
    }
}