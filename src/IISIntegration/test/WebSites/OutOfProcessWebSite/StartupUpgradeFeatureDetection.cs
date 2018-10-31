// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestSites
{
    public class StartupUpgradeFeatureDetection
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async ctx =>
            {
                if (ctx.Features.Get<IHttpUpgradeFeature>() != null)
                {
                    await ctx.Response.WriteAsync("Enabled");
                }
                else
                {
                    await ctx.Response.WriteAsync("Disabled");
                }
            });
        }
    }
}
