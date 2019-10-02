// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MonoSanity
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseFileServer(new FileServerOptions() { EnableDefaultFiles = true, });
            app.UseStaticFiles();
            app.UseClientSideBlazorFiles<MonoSanityClient.Program>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToClientSideBlazor<MonoSanityClient.Program>("index.html");
            });
        }
    }
}
