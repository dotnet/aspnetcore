// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Provides context for web-based persistent component state restoration.
/// </summary>
public sealed class WebPersistenceContext : IPersistentComponentStateScenario
{
    /// <summary>
    /// Initializes a new instance of <see cref="WebPersistenceContext"/>.
    /// </summary>
    /// <param name="reason">The reason for this restoration.</param>
    /// <param name="renderMode">The render mode context for this restoration.</param>
    public WebPersistenceContext(WebPersistenceReason reason, IComponentRenderMode? renderMode = null)
    {
        Reason = reason;
        RenderMode = renderMode;
    }

    /// <summary>
    /// Gets the reason for this restoration.
    /// </summary>
    public WebPersistenceReason Reason { get; }

    /// <summary>
    /// Gets the render mode context for this restoration.
    /// </summary>
    public IComponentRenderMode? RenderMode { get; }

    /// <inheritdoc />
    public bool IsRecurring => Reason == WebPersistenceReason.EnhancedNavigation;

    /// <summary>
    /// Gets a context for enhanced navigation restoration.
    /// </summary>
    public static WebPersistenceContext EnhancedNavigation => new(WebPersistenceReason.EnhancedNavigation);

    /// <summary>
    /// Gets a context for prerendering restoration.
    /// </summary>
    public static WebPersistenceContext Prerendering => new(WebPersistenceReason.Prerendering);

    /// <summary>
    /// Gets a context for reconnection restoration.
    /// </summary>
    public static WebPersistenceContext Reconnection => new(WebPersistenceReason.Reconnection);
}