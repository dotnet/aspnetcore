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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var options = new RewriteOptions()
                .AddRedirect("(.*)/$", "$1")
                .AddRewrite(@"app/(\d+)", "app?id=$1", skipRemainingRules: false)
                .AddRedirectToHttps(302, 5001)
                .AddIISUrlRewrite(env.ContentRootFileProvider, "UrlRewrite.xml")
                .AddApacheModRewrite(env.ContentRootFileProvider, "Rewrite.txt");

            app.UseRewriter(options);

            app.Run(context => context.Response.WriteAsync($"Rewritten Url: {context.Request.Path + context.Request.QueryString}"));
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.UseHttps("testCert.pfx", "testPassword");
                })
                .UseUrls("http://localhost:5000", "https://localhost:5001")
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            host.Run();
        }
    }
}
