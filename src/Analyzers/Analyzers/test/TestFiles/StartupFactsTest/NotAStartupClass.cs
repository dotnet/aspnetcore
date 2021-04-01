// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupFactsTest
{
    public class NotAStartupClass
    {
        // no args - not a ConfigureServices (technically it is, but we exclude this case).
        public void ConfigureServices()
        {
        }

        // extra arg - not a ConfigureServices
        public void ConfigureServices(IServiceCollection services, string x)
        {
        }

        // wrong name - not a ConfigureServices
        public void ConfigureSrvces(IServiceCollection services)
        {
        }

        // non-public - not a ConfigureServices
        internal void ConfigureServices(IServiceCollection services)
        {
        }

        // no IApplicationBuilder - not a Configure
        public void Configure(IConfiguration configuration)
        {
        }

        // wrong prefix - not a Configure
        public void Configur(IApplicationBuilder app)
        {
        }

        // non-public - not a Configure
        internal void Configure(IApplicationBuilder app)
        {
        }
    }
}
