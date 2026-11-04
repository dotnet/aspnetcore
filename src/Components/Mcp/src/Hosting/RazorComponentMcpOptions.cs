// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Mcp;

/// <summary>
/// Configures a statically rendered Razor component application exposed through MCP Apps.
/// </summary>
public sealed class RazorComponentMcpOptions
{
    /// <summary>
    /// Gets or sets the application name. For a generated opening tool, this is also its name.
    /// The default is <c>blazor</c>, or the associated existing tool's name.
    /// </summary>
    public string Name { get; set; } = "blazor";

    /// <summary>
    /// Gets or sets the absolute base URI of the component application.
    /// </summary>
    /// <remarks>
    /// The URI must end with a slash. Component routing and relative asset URLs use this
    /// application URI rather than the MCP transport endpoint or the containing iframe.
    /// </remarks>
    public Uri BaseUri { get; set; } = default!;

    /// <summary>
    /// Gets or sets the initial path, relative to <see cref="BaseUri"/>.
    /// The default is the application base URI.
    /// </summary>
    public string InitialPath { get; set; } = "";

    /// <summary>
    /// Gets or sets whether rendered body updates preserve matching DOM elements.
    /// The default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// This configures the MCP browser runtime. <see cref="ConfigureBrowser"/> does not
    /// configure an MCP application because it requires a page HTTP context.
    /// The document head is synchronized in either mode so the application base URI remains active.
    /// </remarks>
    public bool PreserveDom { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum level of messages emitted by the MCP browser runtime.
    /// The default is <see cref="Microsoft.Extensions.Logging.LogLevel.Information"/>.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
