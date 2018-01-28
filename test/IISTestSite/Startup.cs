// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IIS;

namespace IISTestSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Map("/ServerVariable", ServerVariable);
        }

        private void ServerVariable(IApplicationBuilder app)
        {
            app.Run(async ctx =>
            {
                var varName = ctx.Request.Query["q"];
                await ctx.Response.WriteAsync($"{varName}: {ctx.GetIISServerVariable(varName) ?? "(null)"}");
            });
        }
    }
}
