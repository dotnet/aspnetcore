// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Serializable subset of <c>WebAssemblyStartOptions</c>.
/// Non-serializable options (<c>loadBootResource</c>, <c>configureRuntime</c>,
/// <c>initializers</c>) must use <c>Blazor.start()</c> or JS initializers.
/// </summary>
public sealed class WebAssemblyBrowserOptions
{
    /// <summary>
    /// Gets or sets the hosting environment name (e.g., "Development", "Production").
    /// Maps to <c>WebAssemblyStartOptions.environment</c>.
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// Gets or sets the application culture in BCP 47 format (e.g., "en-US").
    /// Maps to <c>WebAssemblyStartOptions.applicationCulture</c>.
    /// </summary>
    public string? ApplicationCulture { get; set; }

    /// <summary>
    /// Gets the environment variables for the .NET WebAssembly runtime.
    /// Use for OTEL endpoints, service URLs, etc.
    /// </summary>
    public IDictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
}
