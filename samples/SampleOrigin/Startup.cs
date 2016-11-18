// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleOrigin
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.Run( context =>
            {
                var fileInfoProvider = env.WebRootFileProvider;
                var fileInfo = fileInfoProvider.GetFileInfo("/Index.html");
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                return context.Response.SendFileAsync(fileInfo);
            });

            app.Run(async context =>
            {
                await context.Response.WriteAsync("Status code of your request: " + context.Response.StatusCode.ToString());
            });

        }
    }
}
