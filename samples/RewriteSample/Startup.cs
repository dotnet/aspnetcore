// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;

namespace RewriteSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnv)
        {
            app.UseRewriter(new RewriteOptions()
                  .ImportFromUrlRewrite(hostingEnv, "UrlRewrite.xml")
                  .ImportFromModRewrite(hostingEnv, "Rewrite.txt"));
            app.Run(context => context.Response.WriteAsync(context.Request.Path));

        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            host.Run();
        }
    }
}
