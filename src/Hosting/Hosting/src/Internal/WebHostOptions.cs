// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class WebHostOptions
{
    public WebHostOptions(IConfiguration primaryConfiguration, IConfiguration? fallbackConfiguration = null, IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(primaryConfiguration);

        string? GetConfig(string key) => primaryConfiguration[key] ?? fallbackConfiguration?[key];

        ApplicationName = environment?.ApplicationName ?? GetConfig(WebHostDefaults.ApplicationKey) ?? Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        StartupAssembly = GetConfig(WebHostDefaults.StartupAssemblyKey);
        DetailedErrors = WebHostUtilities.ParseBool(GetConfig(WebHostDefaults.DetailedErrorsKey));
        CaptureStartupErrors = WebHostUtilities.ParseBool(GetConfig(WebHostDefaults.CaptureStartupErrorsKey));
        Environment = environment?.EnvironmentName ?? GetConfig(WebHostDefaults.EnvironmentKey);
        WebRoot = GetConfig(WebHostDefaults.WebRootKey);
        ContentRootPath = environment?.ContentRootPath ?? GetConfig(WebHostDefaults.ContentRootKey);
        PreventHostingStartup = WebHostUtilities.ParseBool(GetConfig(WebHostDefaults.PreventHostingStartupKey));
        SuppressStatusMessages = WebHostUtilities.ParseBool(GetConfig(WebHostDefaults.SuppressStatusMessagesKey));
        ServerUrls = GetConfig(WebHostDefaults.ServerUrlsKey);
        PreferHostingUrls = WebHostUtilities.ParseBool(GetConfig(WebHostDefaults.PreferHostingUrlsKey));

        // Search the primary assembly and configured assemblies.
        HostingStartupAssemblies = Split(ApplicationName, GetConfig(WebHostDefaults.HostingStartupAssembliesKey));
        HostingStartupExcludeAssemblies = Split(GetConfig(WebHostDefaults.HostingStartupExcludeAssembliesKey));

        var timeout = GetConfig(WebHostDefaults.ShutdownTimeoutKey);
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

    public TimeSpan ShutdownTimeout { get; } = TimeSpan.FromSeconds(30);

    public string? ServerUrls { get; }

    public bool PreferHostingUrls { get; }

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
