// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class MvcOptions_UseMvcMultiple
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            /*MM1*/app.UseMvcWithDefaultRoute();

            app.UseStaticFiles();
            app.UseMiddleware<AuthorizationMiddleware>();

            /*MM2*/app.UseMvc();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
            });

            /*MM3*/app.UseMvc();
        }
    }
}
