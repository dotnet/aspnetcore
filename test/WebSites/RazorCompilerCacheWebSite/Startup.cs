// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.Framework.DependencyInjection;

namespace RazorCompilerCacheWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc();
            services.AddSingleton<ICompilerCache, CustomCompilerCache>();
            services.AddSingleton<CompilerCacheInitialiedService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvc();
        }
    }
}
