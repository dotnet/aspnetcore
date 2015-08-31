// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;

namespace RazorPageExecutionInstrumentationWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Normalize line endings to avoid changes in instrumentation locations between systems.
            services.TryAdd(ServiceDescriptor.Transient<IRazorCompilationService, TestRazorCompilationService>());

            // Add MVC services to the services container
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.Use(async (HttpContext context, Func<Task> next) =>
            {
                if (!string.IsNullOrEmpty(context.Request.Headers["ENABLE-RAZOR-INSTRUMENTATION"]))
                {
                    var pageExecutionContext = context.ApplicationServices.GetRequiredService<TestPageExecutionContext>();
                    var listenerFeature = new TestPageExecutionListenerFeature(pageExecutionContext);
                    context.Features.Set<IPageExecutionListenerFeature>(listenerFeature);
                }

                await next();
            });

            // Add MVC to the request pipeline
            app.UseMvcWithDefaultRoute();
        }
    }
}
