// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.SignalR.Client.FunctionalTests;

public class VersionStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.RemoveAll<IHubProtocol>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol>(new VersionedJsonHubProtocol(1000)));

        services.AddAuthentication();

        // Since tests run in parallel, it's possible multiple servers will startup,
        // we use an ephemeral key provider and repository to avoid filesystem contention issues
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

        services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new EphemeralXmlRepository();
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<VersionHub>("/version");
        });
    }
}
