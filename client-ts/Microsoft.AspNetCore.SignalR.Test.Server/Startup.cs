// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.SignalR.Test.Server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSockets();
            services.AddSignalR(options => {
                // we are running the same tests with JSON and MsgPack protocols and having
                // consistent casing makes it cleaner to verify results
                options.JsonSerializerSettings.ContractResolver = new DefaultContractResolver();
            });
            services.AddEndPoint<EchoEndPoint>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();
            app.UseSockets(options => options.MapEndPoint<EchoEndPoint>("echo"));
            app.UseSignalR(options => options.MapHub<TestHub>("testhub"));
            app.UseSignalR(options => options.MapHub<UncreatableHub>("uncreatable"));
        }
    }
}
