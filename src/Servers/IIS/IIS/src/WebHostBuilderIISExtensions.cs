// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods for the IIS In-Process.
/// </summary>
public static class WebHostBuilderIISExtensions
{
    /// <summary>
    /// Configures the port and base path the server should listen on when running behind AspNetCoreModule.
    /// The app will also be configured to capture startup errors.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseIIS(this IWebHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        // Check if in process
        if (OperatingSystem.IsWindows() && NativeMethods.IsAspNetCoreModuleLoaded())
        {
            var iisConfigData = NativeMethods.HttpGetApplicationProperties();
            // Trim trailing slash to be consistent with other servers
            var contentRoot = iisConfigData.pwzFullApplicationPath.TrimEnd(Path.DirectorySeparatorChar);
            hostBuilder.UseContentRoot(contentRoot);
            return hostBuilder.ConfigureServices(
                services =>
                {
                    services.AddSingleton(new IISNativeApplication(new NativeSafeHandle(iisConfigData.pNativeApplication)));
                    services.AddSingleton<IServer, IISHttpServer>();
                    services.AddTransient<IISServerAuthenticationHandlerInternal>();
                    services.AddSingleton<IStartupFilter, IISServerSetupFilter>();
                    services.AddAuthenticationCore();
                    services.AddSingleton<IServerIntegratedAuth>(_ => new ServerIntegratedAuth()
                    {
                        IsEnabled = iisConfigData.fWindowsAuthEnabled || iisConfigData.fBasicAuthEnabled,
                        AuthenticationScheme = IISServerDefaults.AuthenticationScheme
                    });
                    services.Configure<IISServerOptions>(
                        options =>
                        {
                            options.ServerAddresses = iisConfigData.pwzBindings.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            options.ForwardWindowsAuthentication = iisConfigData.fWindowsAuthEnabled || iisConfigData.fBasicAuthEnabled;
                            options.MaxRequestBodySize = iisConfigData.maxRequestBodySize;
                            options.IisMaxRequestSizeLimit = iisConfigData.maxRequestBodySize;
                        }
                    );

                    services.TryAddSingleton<IMemoryPoolFactory<byte>, DefaultMemoryPoolFactory>();
                    services.TryAddSingleton<MemoryPoolMetrics>();

                    // Replace default ConsoleLifetime as it can cause shutdown hangs with Preload Enabled = true
                    services.AddSingleton<IHostLifetime, IISHostLifetime>();
                });
        }

        return hostBuilder;
    }

    private sealed class IISHostLifetime : IHostLifetime, IDisposable
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private PosixSignalRegistration? _sigTermRegistration;

        public IISHostLifetime(IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM,
                _ =>
                {
                    // Logic copied from
                    // https://github.com/dotnet/runtime/blob/535c5deba263df5bd4be244247e43bddce288254/src/libraries/Microsoft.Extensions.Hosting/src/Internal/ConsoleLifetime.netcoreapp.cs#L38

                    // don't allow Dispose to unregister handlers, since Windows has a lock that prevents the unregistration while this handler is running
                    // just leak these, since the process is exiting
                    _sigTermRegistration = null;

                    _applicationLifetime.StopApplication();

                    // Equivalent to the pre-10.0 version of ConsoleLifetime
                    // Does not copy Thread.Sleep(HostOptions.ShutdownTimeout)
                    // from ConsoleLifetime because that causes machine shutdown to hang
                    // if Preload Enabled = true
                    // Likely an IIS/WAS issue.
                });
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _sigTermRegistration?.Dispose();
        }
    }
}
