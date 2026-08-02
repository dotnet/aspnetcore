// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.SpaProxy.SpaHostingStartup))]

namespace Microsoft.AspNetCore.SpaProxy;

internal sealed class SpaHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var spaProxyConfigFile = Path.Combine(AppContext.BaseDirectory, "spa.proxy.json");
            if (File.Exists(spaProxyConfigFile))
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(spaProxyConfigFile)
                    .Build();

                services.AddSingleton<SpaProxyLaunchManager>();
                ConfigureOptions<SpaDevelopmentServerOptions>(services, configuration.GetSection("SpaProxyServer"));
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, SpaProxyStartupFilter>());
            }
        });

        [UnconditionalSuppressMessageAttribute("Trimming", "IL2026", Justification = "Configuration object's public properties are preserved.")]
        static void ConfigureOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(IServiceCollection services, IConfigurationSection section)
            where T : class
        {
            services.Configure<T>(section);
        }
    }
}
