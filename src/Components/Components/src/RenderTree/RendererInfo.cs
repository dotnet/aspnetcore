// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides information about the platform that the component is running on.
/// </summary>
/// <param name="rendererType">The Type of the platform.</param>
/// <param name="isInteractive">A flag to indicate if the platform is interactive.</param>
public sealed class RendererInfo(EnumRendererInfo rendererType, bool isInteractive)
{
    /// <summary>
    /// Gets the name of the platform.
    /// </summary>
    [Obsolete("Name is deprecated. Use RenderedType instead.")]
    public string Name => RenderedType.ToString();

    /// <summary>
    /// Gets the type of the Renderer.
    /// </summary>
    public EnumRendererInfo RenderedType => rendererType;

    /// <summary>
    /// Gets a flag to indicate if the platform is interactive.
    /// </summary>
    public bool IsInteractive { get; } = isInteractive;
}

/// <summary>
/// Defines the available rendering environments for Blazor components.
/// </summary>
public enum EnumRendererInfo
{
    /// <summary>
    /// The renderer outputs only static HTML without interactivity.
    /// </summary>
    Static,

    /// <summary>
    /// The renderer is executing during server-side prerendering (SSR).
    /// </summary>
    SSR,

    /// <summary>
    /// The renderer runs in interactive server mode, maintaining a live
    /// SignalR connection between the browser and the server.
    /// </summary>
    Server,

    /// <summary>
    /// The renderer runs directly in the browser using WebAssembly.
    /// </summary>
    WebAssembly,

    /// <summary>
    /// The renderer runs inside a native WebView host (for example, .NET MAUI).
    /// </summary>
    WebView
}
