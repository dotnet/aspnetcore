// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Kestrel <see cref="IWebHostBuilder"/> extensions.
/// </summary>
public static class WebHostBuilderKestrelExtensions
{
    /// <summary>
    /// Specify Kestrel as the server to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            // Don't override an already-configured transport
            services.TryAddSingleton<IConnectionListenerFactory, SocketTransportFactory>();

            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
            services.AddSingleton<IServer, KestrelServerImpl>();
        });

        hostBuilder.UseQuic(options =>
        {
            // Configure server defaults to match client defaults.
            // https://github.com/dotnet/runtime/blob/a5f3676cc71e176084f0f7f1f6beeecd86fbeafc/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/ConnectHelper.cs#L118-L119
            options.DefaultStreamErrorCode = (long)Http3ErrorCode.RequestCancelled;
            options.DefaultCloseErrorCode = (long)Http3ErrorCode.NoError;
        });

        if (OperatingSystem.IsWindows())
        {
            hostBuilder.UseNamedPipes();
        }

        return hostBuilder;
    }

    /// <summary>
    /// Specify Kestrel as the server to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="options">
    /// A callback to configure Kestrel options.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder, Action<KestrelServerOptions> options)
    {
        return hostBuilder.UseKestrel().ConfigureKestrel(options);
    }

    /// <summary>
    /// Configures Kestrel options but does not register an IServer. See <see cref="UseKestrel(IWebHostBuilder)"/>.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="options">
    /// A callback to configure Kestrel options.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder hostBuilder, Action<KestrelServerOptions> options)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>());
            services.Configure(options);
        });
    }

    /// <summary>
    /// Specify Kestrel as the server to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="configureOptions">A callback to configure Kestrel options.</param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, KestrelServerOptions> configureOptions)
    {
        return hostBuilder.UseKestrel().ConfigureKestrel(configureOptions);
    }

    /// <summary>
    /// Configures Kestrel options but does not register an IServer. See <see cref="UseKestrel(IWebHostBuilder)"/>.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <param name="configureOptions">A callback to configure Kestrel options.</param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder ConfigureKestrel(this IWebHostBuilder hostBuilder, Action<WebHostBuilderContext, KestrelServerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>());
            services.Configure<KestrelServerOptions>(options =>
            {
                configureOptions(context, options);
            });
        });
    }
}
