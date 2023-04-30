// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
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
    /// In <see cref="UseKestrelCore(IWebHostBuilder)"/> scenarios, it may be necessary to explicitly
    /// opt in to certain HTTPS functionality.  For example, if <code>ASPNETCORE_URLS</code> includes
    /// an <code>https://</code> address, <see cref="UseKestrelHttpsConfiguration"/> will enable configuration
    /// of HTTPS on that endpoint.
    ///
    /// Has no effect in <see cref="UseKestrel(IWebHostBuilder)"/> scenarios.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseKestrelHttpsConfiguration(this IWebHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<HttpsConfigurationService.IInitializer, HttpsConfigurationService.Initializer>();
        });
    }

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
        return hostBuilder
            .UseKestrelCore()
            .UseKestrelHttpsConfiguration()
            .UseQuic(options =>
            {
                // Configure server defaults to match client defaults.
                // https://github.com/dotnet/runtime/blob/a5f3676cc71e176084f0f7f1f6beeecd86fbeafc/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/ConnectHelper.cs#L118-L119
                options.DefaultStreamErrorCode = (long)Http3ErrorCode.RequestCancelled;
                options.DefaultCloseErrorCode = (long)Http3ErrorCode.NoError;
            });
    }

    /// <summary>
    /// Specify Kestrel as the server to be used by the web host.
    /// Includes less automatic functionality than <see cref="UseKestrel(IWebHostBuilder)"/> to make trimming more effective
    /// (e.g. for <see href="https://aka.ms/aspnet/nativeaot">Native AOT</see> scenarios).  If the host ends up depending on
    /// some of the absent functionality, a best-effort attempt will be made to enable it on-demand.  Failing that, an
    /// exception with an informative error message will be raised when the host is started.
    /// </summary>
    /// <param name="hostBuilder">
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
    /// </param>
    /// <returns>
    /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
    /// </returns>
    public static IWebHostBuilder UseKestrelCore(this IWebHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            // Don't override an already-configured transport
            services.TryAddSingleton<IConnectionListenerFactory, SocketTransportFactory>();

            services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
            services.AddSingleton<IHttpsConfigurationService, HttpsConfigurationService>();
            services.AddSingleton<IServer, KestrelServerImpl>();
            services.AddSingleton<KestrelMetrics>();
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
