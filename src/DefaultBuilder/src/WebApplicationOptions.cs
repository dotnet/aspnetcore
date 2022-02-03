// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for configuing the behavior for <see cref="WebApplication.CreateBuilder(WebApplicationOptions)"/>.
/// </summary>
public class WebApplicationOptions
{
    /// <summary>
    /// The command line arguments.
    /// </summary>
    public string[]? Args { get; init; }

    /// <summary>
    /// The environment name.
    /// </summary>
    public string? EnvironmentName { get; init; }

    /// <summary>
    /// The application name.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// The content root path.
    /// </summary>
    public string? ContentRootPath { get; init; }

    /// <summary>
    /// The web root path.
    /// </summary>
    public string? WebRootPath { get; init; }

    internal void ApplyHostConfiguration(IConfigurationBuilder builder)
    {
        Dictionary<string, string?>? config = null;

        if (EnvironmentName is not null)
        {
            config = new();
            config[HostDefaults.EnvironmentKey] = EnvironmentName;
        }

        if (ApplicationName is not null)
        {
            config ??= new();
            config[HostDefaults.ApplicationKey] = ApplicationName;
        }

        if (ContentRootPath is not null)
        {
            config ??= new();
            config[HostDefaults.ContentRootKey] = ContentRootPath;
        }

        if (WebRootPath is not null)
        {
            config ??= new();
            config[WebHostDefaults.WebRootKey] = WebRootPath;
        }

        if (config is not null)
        {
            builder.AddInMemoryCollection(config);
        }
    }

    internal void ApplyApplicationName(IWebHostBuilder webHostBuilder)
    {
        string? applicationName = null;

        // We need to "parse" the args here since
        // we need to set the application name via UseSetting
        if (Args is not null)
        {
            var config = new ConfigurationBuilder()
                    .AddCommandLine(Args)
                    .Build();

            applicationName = config[WebHostDefaults.ApplicationKey];

            // This isn't super important since we're not adding any disposable sources
            // but just in case
            if (config is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        // Application name overrides args
        if (ApplicationName is not null)
        {
            applicationName = ApplicationName;
        }

        // We need to override the application name since the call to Configure will set it to
        // be the calling assembly's name.
        applicationName ??= Assembly.GetEntryAssembly()?.GetName()?.Name ?? string.Empty;

        webHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, applicationName);
    }
}
