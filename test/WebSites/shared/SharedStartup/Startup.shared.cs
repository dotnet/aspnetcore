// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestSite
{
    public partial class Startup
    {
        private async Task HostingEnvironment(HttpContext ctx)
        {
            var hostingEnv = ctx.RequestServices.GetService<IHostingEnvironment>();

            await ctx.Response.WriteAsync("ContentRootPath " + hostingEnv.ContentRootPath + Environment.NewLine);
            await ctx.Response.WriteAsync("WebRootPath " + hostingEnv.WebRootPath + Environment.NewLine);
            await ctx.Response.WriteAsync("CurrentDirectory " + Environment.CurrentDirectory);
        }

        private async Task ConsoleWrite(HttpContext ctx)
        {
            Console.WriteLine("TEST MESSAGE");

            await ctx.Response.WriteAsync("Hello World");
        }

        private async Task ConsoleErrorWrite(HttpContext ctx)
        {
            Console.Error.WriteLine("TEST MESSAGE");

            await ctx.Response.WriteAsync("Hello World");
        }
    }
}
