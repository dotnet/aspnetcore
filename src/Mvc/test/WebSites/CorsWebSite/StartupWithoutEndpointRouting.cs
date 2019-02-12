// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

namespace CorsWebSite
{
    public class StartupWithoutEndpointRouting : Startup
    {
        public override void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }

        protected override void ConfigureMvcOptions(MvcOptions options)
        {
            options.EnableEndpointRouting = false;
        }
    }
}
