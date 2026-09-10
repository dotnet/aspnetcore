// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// <see cref="IWebHostBuilder"/> extension methods to register the DirectTls transport with Kestrel.
/// </summary>
[Experimental("ASPNETCORE_DIRECTTLS_001", UrlFormat = "https://aka.ms/aspnetcore/directtls")]
public static class WebHostBuilderDirectTlsExtensions
{
    /// <summary>
    /// Registers the DirectTls transport (native, file-descriptor-bound OpenSSL TLS) alongside the default
    /// Kestrel transport. Endpoints bound with a <see cref="DirectTlsEndpoint"/> are served by this transport;
    /// all other endpoints continue to use the default transport.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    /// <remarks>
    /// Call this <b>after</b> <c>UseKestrel()</c> so the DirectTls transport is offered a
    /// <see cref="DirectTlsEndpoint"/> before the default transport.
    /// </remarks>
    [SupportedOSPlatform("linux")]
    public static IWebHostBuilder UseDirectTls(this IWebHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        EnsureCompatibleKestrelCoreVersion();
        if (!OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("The DirectTls transport requires a Linux operating system.");
        }

        return hostBuilder.ConfigureServices(services =>
        {
            // Registered additively (not TryAdd): the default transport is already registered by UseKestrel.
            // KestrelServerImpl reverses the factory list, so this last-registered factory is tried first and
            // its CanBind claims only DirectTlsEndpoint; every other endpoint falls through to the default.
            services.AddSingleton<IConnectionListenerFactory, DirectTlsTransportFactory>();

            services.TryAddSingleton<IMemoryPoolFactory<byte>, DefaultSimpleMemoryPoolFactory>();
            services.AddOptions<DirectTlsTransportOptions>().Configure((DirectTlsTransportOptions options, IMemoryPoolFactory<byte> factory) =>
            {
                // UseKestrelCore normally registers PinnedBlockMemoryPoolFactory. The fallback supports
                // standalone transport registration, matching the sockets transport.
                options.MemoryPoolFactory = factory;
            });

            // Copies each DirectTlsEndpoint's ListenOptions.Protocols onto the endpoint after all endpoints
            // are configured, so the transport can read the ALPN protocols off the endpoint itself.
            services.AddSingleton<IPostConfigureOptions<KestrelServerOptions>, DirectTlsEndpointProtocolsSetup>();

            // Defaults MaxConcurrentHandshakes from KestrelServerLimits.MaxConcurrentConnections when the
            // transport option was not set explicitly, so the pre-handshake flood cap tracks the server's
            // configured connection limit without a separate knob.
            services.AddSingleton<IPostConfigureOptions<DirectTlsTransportOptions>, DirectTlsHandshakeLimitSetup>();
        });
    }

    /// <summary>
    /// Registers the DirectTls transport and configures its transport-wide options.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="configureOptions">A callback to configure <see cref="DirectTlsTransportOptions"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [SupportedOSPlatform("linux")]
    public static IWebHostBuilder UseDirectTls(this IWebHostBuilder hostBuilder, Action<DirectTlsTransportOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return hostBuilder.UseDirectTls().ConfigureServices(services =>
        {
            services.Configure(configureOptions);
        });
    }

    // DirectTls is a standalone package that reaches into Kestrel.Core internals via InternalsVisibleTo, which
    // carry no compatibility guarantee across product versions. Its NuGet package only takes an unversioned
    // Microsoft.AspNetCore.App framework reference, so a DirectTls built for one major could load against a
    // different Kestrel.Core major and fail obscurely (e.g. MissingMethodException). Fail fast and clearly.
    private static void EnsureCompatibleKestrelCoreVersion()
    {
        var directTlsAssembly = typeof(WebHostBuilderDirectTlsExtensions).Assembly;
        var kestrelCoreAssembly = typeof(KestrelServerOptions).Assembly;

        // The product (informational) version is compared, not AssemblyName.Version: shared-framework assemblies
        // like Kestrel.Core pin AssemblyVersion to major.0.0.0, while this standalone package keeps the build's
        // dev sentinel, so only the informational version reflects the real product major on both.
        var error = GetKestrelCoreVersionMismatchError(
            directTlsAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            kestrelCoreAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            directTlsAssembly.GetName().Name,
            kestrelCoreAssembly.GetName().Name);

        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }
    }

    // Returns null when the assemblies are compatible, otherwise a clear error message. Pure (no reflection) so
    // the rule is unit-testable. Fail-open: only a confirmed major difference is reported; an unreadable version
    // (e.g. the informational-version attribute trimmed away) is not evidence of a mismatch, so startup proceeds.
    internal static string? GetKestrelCoreVersionMismatchError(string? directTlsVersion, string? kestrelCoreVersion, string? directTlsName, string? kestrelCoreName)
    {
        if (TryGetMajorProductVersion(directTlsVersion, out int directTlsMajor) &&
            TryGetMajorProductVersion(kestrelCoreVersion, out int kestrelCoreMajor) &&
            directTlsMajor != kestrelCoreMajor)
        {
            return $"The DirectTls transport ({directTlsName} {directTlsVersion}) requires a matching major version of " +
                $"Kestrel.Core ({kestrelCoreName} {kestrelCoreVersion}); it consumes Kestrel.Core internals that are not " +
                "compatible across product versions. Reference a DirectTls package that matches your Microsoft.AspNetCore.App version.";
        }

        return null;
    }

    // Parses the leading major integer from an informational/product version such as "11.0.0-dev" or
    // "11.0.0-preview.6.25361.1+sha". Everything from the first '.' on (including prerelease/metadata) is ignored.
    private static bool TryGetMajorProductVersion(string? informationalVersion, out int major)
    {
        major = 0;
        if (string.IsNullOrEmpty(informationalVersion))
        {
            return false;
        }

        int dot = informationalVersion.IndexOf('.');
        var head = dot < 0 ? informationalVersion : informationalVersion[..dot];
        return int.TryParse(head, out major);
    }
}
