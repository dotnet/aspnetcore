// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods for the IIS Out-Of-Process.
/// </summary>
public static class WebHostBuilderIISExtensions
{
    // These are defined as ASPNETCORE_ environment variables by IIS's AspNetCoreModule.
    private const string ServerPort = "PORT";
    private const string ServerPath = "APPL_PATH";
    private const string PairingToken = "TOKEN";
    private const string IISAuth = "IIS_HTTPAUTH";
    private const string IISWebSockets = "IIS_WEBSOCKETS_SUPPORTED";

    /// <summary>
    /// Configures the port and base path the server should listen on when running behind AspNetCoreModule.
    /// The app will also be configured to capture startup errors.
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IWebHostBuilder UseIISIntegration(this IWebHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        // Check if `UseIISIntegration` was called already
        if (hostBuilder.GetSetting(nameof(UseIISIntegration)) != null)
        {
            return hostBuilder;
        }

        var port = hostBuilder.GetSetting(ServerPort) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{ServerPort}");
        var path = hostBuilder.GetSetting(ServerPath) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{ServerPath}");
        var pairingToken = hostBuilder.GetSetting(PairingToken) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{PairingToken}");
        var iisAuth = hostBuilder.GetSetting(IISAuth) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{IISAuth}");
        var websocketsSupported = hostBuilder.GetSetting(IISWebSockets) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{IISWebSockets}");

        bool isWebSocketsSupported;
        if (!bool.TryParse(websocketsSupported, out isWebSocketsSupported))
        {
            // If the websocket support variable is not set, we will always fallback to assuming websockets are enabled.
            isWebSocketsSupported = (Environment.OSVersion.Version >= new Version(6, 2));
        }

        if (!string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(pairingToken))
        {
            // Set flag to prevent double service configuration
            hostBuilder.UseSetting(nameof(UseIISIntegration), true.ToString());

            var enableAuth = false;
            if (string.IsNullOrEmpty(iisAuth))
            {
                // back compat with older ANCM versions
                enableAuth = true;
            }
            else
            {
                // Lightup a new ANCM variable that tells us if auth is enabled.
                foreach (var authType in iisAuth.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!string.Equals(authType, "anonymous", StringComparison.OrdinalIgnoreCase))
                    {
                        enableAuth = true;
                        break;
                    }
                }
            }

            var address = "http://127.0.0.1:" + port;
            hostBuilder.CaptureStartupErrors(true);

            hostBuilder.ConfigureServices(services =>
            {
                // Delay register the url so users don't accidentally overwrite it.
                hostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, address);
                hostBuilder.PreferHostingUrls(true);
                services.AddSingleton<IServerIntegratedAuth>(_ => new ServerIntegratedAuth()
                {
                    IsEnabled = enableAuth,
                    AuthenticationScheme = IISDefaults.AuthenticationScheme
                });
                services.AddSingleton<IStartupFilter>(new IISSetupFilter(pairingToken, new PathString(path), isWebSocketsSupported));
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                });
                services.Configure<IISOptions>(options =>
                {
                    options.ForwardWindowsAuthentication = enableAuth;
                });
                services.AddAuthenticationCore();
            });
        }

        return hostBuilder;
    }
}
