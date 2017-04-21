// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore
{
    internal class WebHostStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var env = app.ApplicationServices.GetService<IHostingEnvironment>();
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                next(app);
            };
        }
    }
}
