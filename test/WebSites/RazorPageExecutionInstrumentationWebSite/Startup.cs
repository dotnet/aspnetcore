// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace RazorPageExecutionInstrumentationWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Normalize line endings to avoid changes in instrumentation locations between systems.
            services.AddTransient<IRazorCompilationService, TestRazorCompilationService>();

            // Add MVC services to the services container.
            services.AddMvc();

            // Make instrumentation data available in views.
            services.AddScoped<TestPageExecutionListenerFeature, TestPageExecutionListenerFeature>();
            services.AddScoped<IHoldInstrumentationData>(
                provider => provider.GetRequiredService<TestPageExecutionListenerFeature>().Holder);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            // Execute views with instrumentation enabled.
            app.Use((HttpContext context, Func<Task> next) =>
            {
                var listenerFeature = context.RequestServices.GetRequiredService<TestPageExecutionListenerFeature>();
                context.Features.Set<IPageExecutionListenerFeature>(listenerFeature);

                return next();
            });

            // Add MVC to the request pipeline
            app.UseMvcWithDefaultRoute();
        }
    }
}
