// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Cors;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace CorsMiddlewareWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc();
            });

            app.UseCors(policy => policy.WithOrigins("http://example.com"));
            app.UseMvc();
        }
    }
}