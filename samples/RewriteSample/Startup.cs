// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace RewriteSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnv)
        {
            // Four main use cases for Rewrite Options. 
            // 1. Importing from a UrlRewrite file, which are IIS Rewrite rules. 
            // This file is in xml format, starting with the <rewrite> tag. 
            // 2. Importing from a mod_rewrite file, which are mod_rewrite rules.
            // This file is in standard mod_rewrite format which only contains rewrite information.
            // 3. Inline rules in code, where you can specify rules such as rewrites and redirects
            // based on certain conditions. Ex: RedirectToHttps will check if the request is https,
            // else it will redirect the request with https.
            // 4. Functional rules. If a user has a very specific function they would like to implement
            // (ex StringReplace) that are easy to implement in code, they can do so by calling 
            // AddFunctionalRule(Func);
            // TODO make this startup do something useful.
            app.UseRewriter(new RewriteOptions()
                .ImportFromUrlRewrite(hostingEnv, "UrlRewrite.xml")
                .ImportFromModRewrite(hostingEnv, "Rewrite.txt")
                .RedirectToHttps(StatusCodes.Status307TemporaryRedirect)
                .RewriteRule("/foo/(.*)/bar", "{R:1}/bar")
                .AddRule(ctx =>
                {
                    ctx.HttpContext.Request.Path = "/index";
                    return RuleResult.Continue;
                }));

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
