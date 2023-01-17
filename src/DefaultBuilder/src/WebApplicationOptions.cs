// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for configuring the behavior for <see cref="WebApplication.CreateBuilder(WebApplicationOptions)"/>.
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
}
