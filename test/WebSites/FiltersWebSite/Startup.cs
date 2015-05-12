// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace FiltersWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.ConfigureAuthorization(options =>
            {
                // This policy cannot succeed since the claim is never added
                options.AddPolicy("Impossible", policy => policy.RequireClaim("Never"));
                options.AddPolicy("Api", policy =>
                {
                    policy.ActiveAuthenticationSchemes.Add("Api");
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });
                options.AddPolicy("Api-Manager", policy =>
                {
                    policy.ActiveAuthenticationSchemes.Add("Api");
                    policy.Requirements.Add(Operations.Edit);
                });
                options.AddPolicy("Interactive", policy =>
                {
                    policy.ActiveAuthenticationSchemes.Add("Interactive");
                    policy.RequireClaim(ClaimTypes.NameIdentifier)
                          .RequireClaim("Permission", "CanViewPage");
                });
            });
            services.AddSingleton<RandomNumberFilter>();
            services.AddSingleton<RandomNumberService>();
            services.AddTransient<IAuthorizationHandler, ManagerHandler>();
            services.Configure<BasicOptions>(o => o.AuthenticationScheme = "Api", "Api");
            services.Configure<BasicOptions>(o => o.AuthenticationScheme = "Interactive", "Interactive");

            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new GlobalExceptionFilter());
                options.Filters.Add(new GlobalActionFilter());
                options.Filters.Add(new GlobalResultFilter());
                options.Filters.Add(new GlobalAuthorizationFilter());
                options.Filters.Add(new TracingResourceFilter("Global Resource Filter"));
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseErrorReporter();

            app.UseMiddleware<AuthorizeBasicMiddleware>("Interactive");
            app.UseMiddleware<AuthorizeBasicMiddleware>("Api");

            app.UseMvcWithDefaultRoute();
        }
    }
}
