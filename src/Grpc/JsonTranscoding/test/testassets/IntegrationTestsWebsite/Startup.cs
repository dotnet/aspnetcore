// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.AspNetCore.Server.Model;
using IntegrationTestsWebsite.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IntegrationTestsWebsite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
            })
            .AddJsonTranscoding();
        services.AddHttpContextAccessor();

        // When the site is run from the test project these types will be injected
        // This will add a default types if the site is run standalone
        services.TryAddSingleton<DynamicEndpointDataSource>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceMethodProvider<DynamicService>, DynamicServiceModelProvider>());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.DataSources.Add(endpoints.ServiceProvider.GetRequiredService<DynamicEndpointDataSource>());
        });
    }
}
