// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
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

        public async Task Auth(HttpContext ctx)
        {
            var authProvider = ctx.RequestServices.GetService<IAuthenticationSchemeProvider>();
            var authScheme = (await authProvider.GetAllSchemesAsync()).SingleOrDefault();

            await ctx.Response.WriteAsync(authScheme?.Name ?? "null");
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

        public async Task WaitingRequestCount(HttpContext context)
        {
            await context.Response.WriteAsync(_waitingRequestCount.ToString());
        }

        private static bool _applicationInitializationCalled;

        public async Task ApplicationInitialization(HttpContext context)
        {
            if (context.Request.Query["IISInit"] == "true")
            {
                _applicationInitializationCalled = true;
            }
            await context.Response.WriteAsync(_applicationInitializationCalled.ToString());
        }

        public Task OverrideServer(HttpContext context)
        {
            context.Response.Headers["Server"] = "MyServer/7.8";
            return Task.CompletedTask;
        }
    }
}
