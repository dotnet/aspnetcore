// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSockets();
            services.AddSignalR();
            services.AddSingleton<EchoEndPoint>();
            services.AddSingleton<HttpHeaderEndPoint>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSockets(options => options.MapEndPoint<EchoEndPoint>("/echo"));
            app.UseSockets(options => options.MapEndPoint<HttpHeaderEndPoint>("/httpheader"));
            app.UseSignalR(options => options.MapHub<UncreatableHub>("/uncreatable"));
        }
    }
}
