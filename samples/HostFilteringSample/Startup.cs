// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HostFilteringSample
{
    public class Startup
    {
        public IConfiguration Config { get; }

        public Startup(IConfiguration config)
        {
            Config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostFiltering(options =>
            {
                // If this is excluded then it will fall back to the server's addresses
                options.AllowedHosts = Config.GetSection("AllowedHosts").Get<List<string>>();
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHostFiltering();

            app.Run(context =>
            {
                return context.Response.WriteAsync("Hello World! " + context.Request.Host);
            });
        }
    }
}
