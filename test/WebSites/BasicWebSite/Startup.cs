// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
            services
                .AddMvc(options =>  options.Conventions.Add(new ApplicationDescription("This is a basic website.")))
                .AddXmlDataContractSerializerFormatters();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                var previous = options.InvalidModelStateResponseFactory;
                options.InvalidModelStateResponseFactory = context =>
                {
                    var result = (BadRequestObjectResult) previous(context);
                    if (context.ActionDescriptor.FilterDescriptors.Any(f => f.Filter is VndErrorAttribute))
                    {
                        result.ContentTypes.Clear();
                        result.ContentTypes.Add("application/vnd.error+json");
                    }

                    return result;
                };
            });

            services.AddLogging();
            services.AddSingleton<IActionDescriptorProvider, ActionDescriptorCreationCounter>();
            services.AddHttpContextAccessor();
            services.AddSingleton<ContactsRepository>();
            services.AddSingleton<IErrorDescriptorProvider, VndErrorDescriptionProvider>();
            services.AddScoped<RequestIdService>();
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
