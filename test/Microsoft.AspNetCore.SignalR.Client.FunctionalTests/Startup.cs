// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<TestHub>("default");
                routes.MapHub<DynamicTestHub>("dynamic");
                routes.MapHub<TestHubT>("hubT");
            });
        }
    }
}
