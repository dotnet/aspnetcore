// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
