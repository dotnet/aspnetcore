// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestSite
{
    public partial class Startup
    {
        public void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddResponseCompression();
        }

        private async Task ContentRootPath(HttpContext ctx) => await ctx.Response.WriteAsync(ctx.RequestServices.GetService<IHostingEnvironment>().ContentRootPath);

        private async Task WebRootPath(HttpContext ctx) => await ctx.Response.WriteAsync(ctx.RequestServices.GetService<IHostingEnvironment>().WebRootPath);

        private async Task CurrentDirectory(HttpContext ctx) => await ctx.Response.WriteAsync(Environment.CurrentDirectory);

        private async Task BaseDirectory(HttpContext ctx) => await ctx.Response.WriteAsync(AppContext.BaseDirectory);

        private async Task ASPNETCORE_IIS_PHYSICAL_PATH(HttpContext ctx) => await ctx.Response.WriteAsync(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH"));

        private async Task ServerAddresses(HttpContext ctx)
        {
            var serverAddresses = ctx.RequestServices.GetService<IServer>().Features.Get<IServerAddressesFeature>();
            await ctx.Response.WriteAsync(string.Join(",", serverAddresses.Addresses));
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

        public async Task Auth(HttpContext ctx)
        {
            var authProvider = ctx.RequestServices.GetService<IAuthenticationSchemeProvider>();
            var authScheme = (await authProvider.GetAllSchemesAsync()).SingleOrDefault();

            await ctx.Response.WriteAsync(authScheme?.Name ?? "null");
            if (ctx.User.Identity.Name != null)
            {
                await ctx.Response.WriteAsync(":" + ctx.User.Identity.Name);
            }
        }

        public async Task GetClientCert(HttpContext context)
        {
            var clientCert = context.Connection.ClientCertificate;
            await context.Response.WriteAsync(clientCert != null ? $"Enabled;{clientCert.GetCertHashString()}" : "Disabled");
        }

        private static int _waitingRequestCount;

        public Task WaitForAbort(HttpContext context)
        {
            Interlocked.Increment(ref _waitingRequestCount);
            try
            {
                context.RequestAborted.WaitHandle.WaitOne();
                return Task.CompletedTask;
            }
            finally
            {
                Interlocked.Decrement(ref _waitingRequestCount);
            }
        }

        public Task Abort(HttpContext context)
        {
            context.Abort();
            return Task.CompletedTask;
        }

        public async Task WaitingRequestCount(HttpContext context)
        {
            await context.Response.WriteAsync(_waitingRequestCount.ToString());
        }

        public Task CreateFile(HttpContext context)
        {
            var hostingEnv = context.RequestServices.GetService<IHostingEnvironment>();
            File.WriteAllText(System.IO.Path.Combine(hostingEnv.ContentRootPath, "Started.txt"), "");
            return Task.CompletedTask;
        }

        public Task OverrideServer(HttpContext context)
        {
            context.Response.Headers["Server"] = "MyServer/7.8";
            return Task.CompletedTask;
        }

        public void CompressedData(IApplicationBuilder builder)
        {
            builder.UseResponseCompression();
            // write random bytes to check that compressed data is passed through
            builder.Run(
                async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.Body.WriteAsync(new byte[100], 0, 100);
                });
        }

        [DllImport("kernel32.dll")]
        static extern uint GetDllDirectory(uint nBufferLength, [Out] StringBuilder lpBuffer);

        private async Task DllDirectory(HttpContext context)
        {
            var builder = new StringBuilder(1024);
            GetDllDirectory(1024, builder);
            await context.Response.WriteAsync(builder.ToString());
        }

        private async Task GetEnvironmentVariable(HttpContext ctx)
        {
            await ctx.Response.WriteAsync(Environment.GetEnvironmentVariable(ctx.Request.Query["name"].ToString()));
        }
    }
}
