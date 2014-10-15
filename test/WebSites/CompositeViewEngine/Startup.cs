// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace CompositeViewEngine
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                // Add a view engine as the first one in the list.
                services.AddMvc(configuration)
                    .Configure<MvcOptions>(options =>
                    {
                        options.ViewEngines.Insert(0, typeof(TestViewEngine));
                    });
            });

            // Add MVC to the request pipeline
            app.UseMvc();
        }
    }
}
