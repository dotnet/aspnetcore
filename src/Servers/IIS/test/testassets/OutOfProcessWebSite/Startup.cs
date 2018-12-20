// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TestSite
{
    public partial class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            TestStartup.Register(app, this);
        }

        public Task Path(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Request.Path.Value);

        public Task Query(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Request.QueryString.Value);

        public Task BodyLimit(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize?.ToString() ?? "null");

        public Task HelloWorld(HttpContext ctx) => ctx.Response.WriteAsync("Hello World");

        public Task HttpsHelloWorld(HttpContext ctx) =>
            ctx.Response.WriteAsync("Scheme:" + ctx.Request.Scheme + "; Original:" + ctx.Request.Headers["x-original-proto"]);

        public Task Anonymous(HttpContext context) => context.Response.WriteAsync("Anonymous?" + !context.User.Identity.IsAuthenticated);

        public Task Restricted(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                Assert.IsType<WindowsPrincipal>(context.User);
                return context.Response.WriteAsync(context.User.Identity.AuthenticationType);
            }
            else
            {
                return context.ChallengeAsync(IISDefaults.AuthenticationScheme);
            }
        }

        public Task Forbidden(HttpContext context) => context.ForbidAsync(IISDefaults.AuthenticationScheme);

        public Task RestrictedNTLM(HttpContext context)
        {
            if (string.Equals("NTLM", context.User.Identity.AuthenticationType, StringComparison.Ordinal))
            {
                return context.Response.WriteAsync("NTLM");
            }
            else
            {
                return context.ChallengeAsync(IISDefaults.AuthenticationScheme);
            }
        }

        public Task UpgradeFeatureDetection(HttpContext context) =>
            context.Response.WriteAsync(context.Features.Get<IHttpUpgradeFeature>() != null? "Enabled": "Disabled");

        public Task CheckRequestHandlerVersion(HttpContext context)
        {
            // We need to check if the aspnetcorev2_outofprocess dll is loaded by iisexpress.exe
            // As they aren't in the same process, we will try to delete the file and expect a file
            // in use error
            try
            {
                File.Delete(context.Request.Headers["ANCMRHPath"]);
            }
            catch(UnauthorizedAccessException)
            {
                // TODO calling delete on the file will succeed when running with IIS
                return context.Response.WriteAsync("Hello World");
            }

            return context.Response.WriteAsync(context.Request.Headers["ANCMRHPath"]);
        }

        private async Task ProcessId(HttpContext context)
        {
            await context.Response.WriteAsync(Process.GetCurrentProcess().Id.ToString());
        }

        public async Task HTTPS_PORT(HttpContext context)
        {
            var httpsPort = context.RequestServices.GetService<IConfiguration>().GetValue<int?>("HTTPS_PORT");

            await context.Response.WriteAsync(httpsPort.HasValue ? httpsPort.Value.ToString() : "NOVALUE");
        }
    }
}
