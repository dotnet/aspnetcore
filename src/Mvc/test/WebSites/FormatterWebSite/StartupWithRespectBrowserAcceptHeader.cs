// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FormatterWebSite
{
    public class StartupWithRespectBrowserAcceptHeader
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.RespectBrowserAcceptHeader = true;
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}

