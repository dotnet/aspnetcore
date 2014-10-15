// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class StartupUseServices
    {
        public StartupUseServices()
        {
        }

        public void ConfigureUseServicesServices(IServiceCollection services)
        {
            services.Configure<FakeOptions>(o => o.Configured = true);
            services.AddTransient<IFakeService, FakeService>();
        }

        public void Configure(IApplicationBuilder builder)
        {
            builder.UseServices(services =>
            {
                services.AddTransient<FakeService>();
                services.Configure<FakeOptions>(o => o.Message = "Configured");
            });
        }
    }
}