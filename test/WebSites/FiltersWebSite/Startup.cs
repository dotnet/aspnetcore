// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FiltersWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication()
                    .AddScheme<BasicOptions, BasicAuthenticationHandler>("Interactive", _ => { })
                    .AddScheme<BasicOptions, BasicAuthenticationHandler>("Api", _ => { });
            services.AddMvc();
            services.AddAuthorization(options =>
            {
                // This policy cannot succeed since the claim is never added
                options.AddPolicy("Impossible", policy =>
                {
                    policy.AuthenticationSchemes.Add("Interactive");
                    policy.RequireClaim("Never");
                });
                options.AddPolicy("Api", policy =>
                {
                    policy.AuthenticationSchemes.Add("Api");
                    policy.RequireClaim(ClaimTypes.NameIdentifier);
                });
                options.AddPolicy("Api-Manager", policy =>
                {
                    policy.AuthenticationSchemes.Add("Api");
                    policy.Requirements.Add(Operations.Edit);
                });
                options.AddPolicy("Interactive", policy =>
                {
                    policy.AuthenticationSchemes.Add("Interactive");
                    policy.RequireClaim(ClaimTypes.NameIdentifier)
                          .RequireClaim("Permission", "CanViewPage");
                });
            });
            services.AddSingleton<RandomNumberFilter>();
            services.AddSingleton<RandomNumberService>();
            services.AddTransient<IAuthorizationHandler, ManagerHandler>();

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
            app.UseMiddleware<ErrorReporterMiddleware>();

            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration()
                .Build();

            host.Run();
        }
    }
}

