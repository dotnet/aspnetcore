// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Api", _ => { });
            services.AddTransient<IAuthorizationHandler, ManagerHandler>();

            services
                .AddMvc(options =>
                {
                    options.Conventions.Add(new ApplicationDescription("This is a basic website."));
                    // Filter that records a value in HttpContext.Items
                    options.Filters.Add(new TraceResourceFilter());

                    // Remove when all URL generation tests are passing - https://github.com/aspnet/Routing/issues/590
                    options.EnableEndpointRouting = false;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddXmlDataContractSerializerFormatters();

            services.ConfigureBaseWebSiteAuthPolicies();

            services.AddTransient<IAuthorizationHandler, ManagerHandler>();

            services.AddLogging();
            services.AddSingleton<IActionDescriptorProvider, ActionDescriptorCreationCounter>();
            services.AddHttpContextAccessor();
            services.AddSingleton<ContactsRepository>();
            services.AddScoped<RequestIdService>();
            services.AddTransient<ServiceActionFilter>();
            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseStaticFiles();

            // Initializes the RequestId service for each request
            app.UseMiddleware<RequestIdMiddleware>();

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "areaRoute",
                    "{area:exists}/{controller}/{action}",
                    new { controller = "Home", action = "Index" });

                routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute("PageRoute", "{controller}/{action}/{page}");
            });
        }
    }
}
