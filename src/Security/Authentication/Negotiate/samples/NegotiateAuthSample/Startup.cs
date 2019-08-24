// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Negotiate;
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
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = options.DefaultPolicy;
            });
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate(options =>
                {
                    options.Events = new NegotiateEvents()
                    {
                        OnAuthenticationFailed = context =>
                        {
                            // context.SkipHandler();
                            return Task.CompletedTask;
                        }
                    };
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Run(HandleRequest);
        }

        public async Task HandleRequest(HttpContext context)
        {
            var user = context.User.Identity;
            await context.Response.WriteAsync($"Authenticated? {user.IsAuthenticated}, Name: {user.Name}, Protocol: {context.Request.Protocol}");
        }
    }
}
