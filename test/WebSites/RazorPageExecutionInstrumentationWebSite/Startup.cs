// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor.Compilation;
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
        }

        public void Configure(IApplicationBuilder app)
        {
            var listener = new RazorPageDiagnosticListener();
            var diagnosticSource = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
            diagnosticSource.SubscribeWithAdapter(listener);

            app.UseCultureReplacer();

            app.Use(async (context, next) =>
            {
                using (var writer = new StreamWriter(context.Response.Body))
                {
                    context.Items[RazorPageDiagnosticListener.WriterKey] = writer;
                    context.Response.Body = Stream.Null;
                    await next();
                }
            });

            // Add MVC to the request pipeline
            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
