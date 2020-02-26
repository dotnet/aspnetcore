// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    class StartupCaseInsensitive
    {
        public static IServiceProvider ConfigureCaseInsensitiveServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<FakeOptions>(o =>
            {
                o.Configured = true;
                o.Environment = "ConfigureCaseInsensitiveServices";
            });
            return services.BuildServiceProvider();
        }

        public void ConfigureCaseInsensitive(IApplicationBuilder app)
        {
        }
    }
}
