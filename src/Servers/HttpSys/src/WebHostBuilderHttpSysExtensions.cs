// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Provides extensions method to use Http.sys as the server for the web host.
/// </summary>
public static class WebHostBuilderHttpSysExtensions
{
    /// <summary>
    /// Specify Http.sys as the server to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <returns>
    /// A reference to the <see cref="IWebHostBuilder" /> parameter object.
    /// </returns>
    [SupportedOSPlatform("windows")]
    public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<MessagePump>();
            services.AddSingleton<IServer>(services => services.GetRequiredService<MessagePump>());
            if (HttpApi.SupportsDelegation)
            {
                services.AddSingleton<IServerDelegationFeature>(services => services.GetRequiredService<MessagePump>());
            }
            services.AddTransient<AuthenticationHandler>();
            services.AddSingleton<IServerIntegratedAuth>(services =>
            {
                var options = services.GetRequiredService<IOptions<HttpSysOptions>>().Value;
                return new ServerIntegratedAuth()
                {
                    IsEnabled = options.Authentication.Schemes != AuthenticationSchemes.None,
                    AuthenticationScheme = HttpSysDefaults.AuthenticationScheme,
                };
            });
            services.AddAuthenticationCore();
        });
    }

    /// <summary>
    /// Specify Http.sys as the server to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="options">
    /// A callback to configure Http.sys options.
    /// </param>
    /// <returns>
    /// A reference to the <see cref="IWebHostBuilder" /> parameter object.
    /// </returns>
    [SupportedOSPlatform("windows")]
    public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder, Action<HttpSysOptions> options)
    {
        return hostBuilder.UseHttpSys().ConfigureServices(services =>
        {
            services.Configure(options);
        });
    }
}
