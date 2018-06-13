// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.IISIntegration.FunctionalTests;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TestSites
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            TestStartup.Register(app, this);
        }

        public Task Path(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Request.Path.Value);

        public Task Query(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Request.QueryString.Value);

        public Task BodyLimit(HttpContext ctx) => ctx.Response.WriteAsync(ctx.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize?.ToString() ?? "null");

        public async Task Auth(HttpContext ctx)
        {
            var iisAuth = Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH");
            var authProvider = ctx.RequestServices.GetService<IAuthenticationSchemeProvider>();
            var authScheme = (await authProvider.GetAllSchemesAsync()).SingleOrDefault();
            if (string.IsNullOrEmpty(iisAuth))
            {
                await ctx.Response.WriteAsync("backcompat;" + (authScheme?.Name ?? "null"));
            }
            else
            {
                await ctx.Response.WriteAsync("latest;" + (authScheme?.Name ?? "null"));
            }
        }

        public Task HelloWorld(HttpContext ctx) => ctx.Response.WriteAsync("Hello World");

        public Task HttpsHelloWorld(HttpContext ctx) =>
            ctx.Response.WriteAsync("Scheme:" + ctx.Request.Scheme + "; Original:" + ctx.Request.Headers["x-original-proto"]);

        public Task CheckClientCert(HttpContext ctx) =>
            ctx.Response.WriteAsync("Scheme:" + ctx.Request.Scheme + "; Original:" + ctx.Request.Headers["x-original-proto"]
                                                   + "; has cert? " + (ctx.Connection.ClientCertificate != null));

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
                return context.Response.WriteAsync("Hello World");
            }

            return context.Response.WriteAsync(context.Request.Headers["ANCMRHPath"]);
        }
    }
}
