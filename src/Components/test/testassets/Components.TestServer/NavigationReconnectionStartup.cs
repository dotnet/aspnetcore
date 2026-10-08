// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TestServer;

public class NavigationReconnectionStartup(IConfiguration configuration) : ServerStartup(configuration)
{
    public new void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // Allow reconnection before the real interop timeout, without waiting for the default minute.
        services.Configure<CircuitOptions>(options => options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(10));
        services.AddScoped<NavigationReconnectionCircuitHandler>();
        services.AddScoped<CircuitHandler>(services => services.GetRequiredService<NavigationReconnectionCircuitHandler>());
        services.AddLogging(logging => logging.AddFilter(
            "Microsoft.AspNetCore.Components.Server.Circuits.RemoteNavigationManager", LogLevel.Debug));
    }
}
