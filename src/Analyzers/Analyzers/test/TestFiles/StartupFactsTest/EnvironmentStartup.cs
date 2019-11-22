// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupFactsTest
{
    public class EnvironmentStartup
    {
        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
        }

        public void configurePRODUCTIONservices(IServiceCollection services)
        {
        }

        // Yes, this is technically a Configure method - if you have an Enviroment called DevelopmentServices2.
        public static void ConfigureDevelopmentServices2(IConfiguration configuration, ILogger logger, IApplicationBuilder app)
        {
        }

        public static void configurePRODUCTION(IConfiguration configuration, ILogger logger, IApplicationBuilder app)
        {
        }
    }
}
