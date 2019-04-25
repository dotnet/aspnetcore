// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NegotiateAuthSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            /* https://github.com/aspnet/AspNetCore/issues/9583
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            */
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseAuthentication();
            // app.UseAuthorization();
            app.Use(async (context, next) =>
            {
                // todo: move to authz https://github.com/aspnet/AspNetCore/issues/9583
                var result = await context.AuthenticateAsync();
                if (!result.Succeeded || !result.Principal.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync();
                    return;
                }
                context.User = result.Principal;

                await next();
            });

            // TODO: Move to endpoints?
            app.Run(HandleRequest);
        }

        public async Task HandleRequest(HttpContext context)
        {
            var user = context.User.Identity;
            await context.Response.WriteAsync($"Authenticated? {user.IsAuthenticated}, Name: {user.Name}");
        }
    }
}
