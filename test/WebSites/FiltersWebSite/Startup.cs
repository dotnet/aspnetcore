// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace FiltersWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
                services.AddSingleton<RandomNumberFilter>();
                services.AddSingleton<RandomNumberService>();

                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new GlobalExceptionFilter());
                });
            });

            app.UseMvc();
        }
    }
}
