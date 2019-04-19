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
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            */
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate(o =>
                {
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseAuthentication();
            // app.UseAuthorization();
            app.Use(async (context, next) =>
            {
                // todo: move to authz
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
