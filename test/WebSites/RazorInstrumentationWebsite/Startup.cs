// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.PageExecutionInstrumentation;
using Microsoft.Framework.DependencyInjection;

namespace RazorInstrumentationWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc(configuration);
            });

            app.Use(async (HttpContext context, Func<Task> next) =>
            {
                if (!string.IsNullOrEmpty(context.Request.Headers["ENABLE-RAZOR-INSTRUMENTATION"]))
                {
                    var pageExecutionContext = context.ApplicationServices.GetRequiredService<TestPageExecutionContext>();
                    var listenerFeature = new TestPageExecutionListenerFeature(pageExecutionContext);
                    context.SetFeature<IPageExecutionListenerFeature>(listenerFeature);
                }

                await next();
            });

            // Add MVC to the request pipeline
            app.UseMvc();
        }
    }
}
