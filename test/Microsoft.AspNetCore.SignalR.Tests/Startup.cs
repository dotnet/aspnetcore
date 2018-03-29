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
            services.AddConnections();
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseConnections(routes =>
            {
                routes.MapConnectionHandler<EchoConnectionHandler>("/echo");
                routes.MapConnectionHandler<WriteThenCloseConnectionHandler>("/echoAndClose");
                routes.MapConnectionHandler<HttpHeaderConnectionHandler>("/httpheader");
            });

            app.UseSignalR(routes =>
            {
                routes.MapHub<UncreatableHub>("/uncreatable");
            });
        }
    }
}
