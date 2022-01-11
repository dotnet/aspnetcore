// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting;

internal class WebHostOptions
{
    public WebHostOptions(IConfiguration configuration, string applicationNameFallback)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        ApplicationName = configuration[WebHostDefaults.ApplicationKey] ?? applicationNameFallback;
        StartupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];
        DetailedErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
        CaptureStartupErrors = WebHostUtilities.ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
        Environment = configuration[WebHostDefaults.EnvironmentKey];
        WebRoot = configuration[WebHostDefaults.WebRootKey];
        ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
        PreventHostingStartup = WebHostUtilities.ParseBool(configuration, WebHostDefaults.PreventHostingStartupKey);
        SuppressStatusMessages = WebHostUtilities.ParseBool(configuration, WebHostDefaults.SuppressStatusMessagesKey);
        ServerUrls = configuration[WebHostDefaults.ServerUrlsKey];

        // Search the primary assembly and configured assemblies.
        HostingStartupAssemblies = Split(ApplicationName, configuration[WebHostDefaults.HostingStartupAssembliesKey]);
        HostingStartupExcludeAssemblies = Split(configuration[WebHostDefaults.HostingStartupExcludeAssembliesKey]);

        var timeout = configuration[WebHostDefaults.ShutdownTimeoutKey];
        if (!string.IsNullOrEmpty(timeout)
            && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
        {
            ShutdownTimeout = TimeSpan.FromSeconds(seconds);
        }
    }

    public string ApplicationName { get; }

    public bool PreventHostingStartup { get; }

    public bool SuppressStatusMessages { get; }

    public IReadOnlyList<string> HostingStartupAssemblies { get; }

    public IReadOnlyList<string> HostingStartupExcludeAssemblies { get; }

    public bool DetailedErrors { get; }

    public bool CaptureStartupErrors { get; }

    public string? Environment { get; }

    public string? StartupAssembly { get; }

    public string? WebRoot { get; }

    public string? ContentRootPath { get; }

    public TimeSpan ShutdownTimeout { get; } = TimeSpan.FromSeconds(5);

    public string? ServerUrls { get; }

    public IEnumerable<string> GetFinalHostingStartupAssemblies()
    {
        return HostingStartupAssemblies.Except(HostingStartupExcludeAssemblies, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> Split(string? value)
    {
        return value?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            ?? Array.Empty<string>();
    }

    private static IReadOnlyList<string> Split(string applicationName, string? environment)
    {
        if (string.IsNullOrEmpty(environment))
        {
            return new[] { applicationName };
        }

        return Split($"{applicationName};{environment}");
    }
}
