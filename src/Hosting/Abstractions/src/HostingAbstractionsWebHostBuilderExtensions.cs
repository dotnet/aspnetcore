// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Contains extension methods for configuring the <see cref="IWebHostBuilder" />.
/// </summary>
public static class HostingAbstractionsWebHostBuilderExtensions
{
    /// <summary>
    /// Use the given configuration settings on the web host.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> containing settings to be used.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseConfiguration(this IWebHostBuilder hostBuilder, IConfiguration configuration)
    {
        foreach (var setting in configuration.AsEnumerable(makePathsRelative: true))
        {
            hostBuilder.UseSetting(setting.Key, setting.Value);
        }

        return hostBuilder;
    }

    /// <summary>
    /// Set whether startup errors should be captured in the configuration settings of the web host.
    /// When enabled, startup exceptions will be caught and an error page will be returned. If disabled, startup exceptions will be propagated.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="captureStartupErrors"><c>true</c> to use startup error page; otherwise <c>false</c>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder CaptureStartupErrors(this IWebHostBuilder hostBuilder, bool captureStartupErrors)
    {
        return hostBuilder.UseSetting(WebHostDefaults.CaptureStartupErrorsKey, captureStartupErrors ? "true" : "false");
    }

    /// <summary>
    /// Specify the assembly containing the startup type to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="startupAssemblyName">The name of the assembly containing the startup type.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    [RequiresUnreferencedCode("This API searches the specified assembly for a startup type using reflection. The startup type may be trimmed. Please use UseStartup<TStartup>() to specify the startup type explicitly.")]
    public static IWebHostBuilder UseStartup(this IWebHostBuilder hostBuilder, string startupAssemblyName)
    {
        ArgumentNullException.ThrowIfNull(startupAssemblyName);

        return hostBuilder
            .UseSetting(WebHostDefaults.ApplicationKey, startupAssemblyName)
            .UseSetting(WebHostDefaults.StartupAssemblyKey, startupAssemblyName);
    }

    /// <summary>
    /// Specify the server to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="server">The <see cref="IServer"/> to be used.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseServer(this IWebHostBuilder hostBuilder, IServer server)
    {
        ArgumentNullException.ThrowIfNull(server);

        return hostBuilder.ConfigureServices(services =>
        {
            // It would be nicer if this was transient but we need to pass in the
            // factory instance directly
            services.AddSingleton(server);
        });
    }

    /// <summary>
    /// Specify the environment to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="environment">The environment to host the application in.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseEnvironment(this IWebHostBuilder hostBuilder, string environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return hostBuilder.UseSetting(WebHostDefaults.EnvironmentKey, environment);
    }

    /// <summary>
    /// Specify the content root directory to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="contentRoot">Path to root directory of the application.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseContentRoot(this IWebHostBuilder hostBuilder, string contentRoot)
    {
        ArgumentNullException.ThrowIfNull(contentRoot);

        return hostBuilder.UseSetting(WebHostDefaults.ContentRootKey, contentRoot);
    }

    /// <summary>
    /// Specify the webroot directory to be used by the web host.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="webRoot">Path to the root directory used by the web server.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseWebRoot(this IWebHostBuilder hostBuilder, string webRoot)
    {
        ArgumentNullException.ThrowIfNull(webRoot);

        return hostBuilder.UseSetting(WebHostDefaults.WebRootKey, webRoot);
    }

    /// <summary>
    /// Specify the urls the web host will listen on.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="urls">The urls the hosted application will listen on.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseUrls(this IWebHostBuilder hostBuilder, [StringSyntax(StringSyntaxAttribute.Uri)] params string[] urls)
    {
        ArgumentNullException.ThrowIfNull(urls);

        return hostBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Join(';', urls));
    }

    /// <summary>
    /// Indicate whether the host should listen on the URLs configured on the <see cref="IWebHostBuilder"/>
    /// instead of those configured on the <see cref="IServer"/>.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="preferHostingUrls"><c>true</c> to prefer URLs configured on the <see cref="IWebHostBuilder"/>; otherwise <c>false</c>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder PreferHostingUrls(this IWebHostBuilder hostBuilder, bool preferHostingUrls)
    {
        return hostBuilder.UseSetting(WebHostDefaults.PreferHostingUrlsKey, preferHostingUrls ? "true" : "false");
    }

    /// <summary>
    /// Specify if startup status messages should be suppressed.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="suppressStatusMessages"><c>true</c> to suppress writing of hosting startup status messages; otherwise <c>false</c>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder SuppressStatusMessages(this IWebHostBuilder hostBuilder, bool suppressStatusMessages)
    {
        return hostBuilder.UseSetting(WebHostDefaults.SuppressStatusMessagesKey, suppressStatusMessages ? "true" : "false");
    }

    /// <summary>
    /// Specify the amount of time to wait for the web host to shutdown.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
    /// <param name="timeout">The amount of time to wait for server shutdown.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseShutdownTimeout(this IWebHostBuilder hostBuilder, TimeSpan timeout)
    {
        return hostBuilder.UseSetting(WebHostDefaults.ShutdownTimeoutKey, ((int)timeout.TotalSeconds).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Start the web host and listen on the specified urls.
    /// </summary>
    /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to start.</param>
    /// <param name="urls">The urls the hosted application will listen on.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHost Start(this IWebHostBuilder hostBuilder, [StringSyntax(StringSyntaxAttribute.Uri)] params string[] urls)
    {
        var host = hostBuilder.UseUrls(urls).Build();
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        return host;
    }
}
