// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

public static partial class WebHostBuilderKestrelExtensions
{
    /// <summary>
    /// [EXPERIMENTAL] Configures Kestrel to use the DirectSsl transport.
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IWebHostBuilder UseKestrelDirectSslTransport(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.UseDirectSslSocketTransport();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
            services.AddSingleton<IHttpsConfigurationService, HttpsConfigurationService>();
            services.AddSingleton<IServer, KestrelServerImpl>();
            services.AddSingleton<KestrelMetrics>();

            services.AddSingleton<PinnedBlockMemoryPoolFactory>();
            services.AddSingleton<MemoryPoolMetrics>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHeartbeatHandler, PinnedBlockMemoryPoolFactory>(sp => sp.GetRequiredService<PinnedBlockMemoryPoolFactory>()));
            services.AddSingleton<IMemoryPoolFactory<byte>>(sp => sp.GetRequiredService<PinnedBlockMemoryPoolFactory>());
        });

        return hostBuilder;
    }
}
