// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Provides scenario context for web-based persistent component state restoration.
/// </summary>
public sealed class WebPersistenceScenario : IPersistentComponentStateScenario
{
    /// <summary>
    /// Gets the render mode context for this restoration.
    /// </summary>
    public IComponentRenderMode? RenderMode { get; }

    /// <summary>
    /// Gets the scenario type.
    /// </summary>
    internal ScenarioType Type { get; }

    /// <inheritdoc />
    bool IPersistentComponentStateScenario.IsRecurring => Type == ScenarioType.EnhancedNavigation;

    private WebPersistenceScenario(ScenarioType type, IComponentRenderMode? renderMode)
    {
        Type = type;
        RenderMode = renderMode;
    }

    /// <summary>
    /// Creates a scenario for enhanced navigation with optional render mode.
    /// </summary>
    /// <param name="renderMode">The render mode context for this restoration.</param>
    /// <returns>A new enhanced navigation scenario.</returns>
    public static WebPersistenceScenario EnhancedNavigation(IComponentRenderMode? renderMode = null)
        => new(ScenarioType.EnhancedNavigation, renderMode);

    /// <summary>
    /// Creates a scenario for prerendering.
    /// </summary>
    /// <returns>A new prerendering scenario.</returns>
    public static WebPersistenceScenario Prerendering()
        => new(ScenarioType.Prerendering, renderMode: null);

    /// <summary>
    /// Creates a scenario for server reconnection.
    /// </summary>
    /// <returns>A new reconnection scenario.</returns>
    public static WebPersistenceScenario Reconnection()
        => new(ScenarioType.Reconnection, renderMode: null);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is WebPersistenceScenario other &&
               Type == other.Type &&
               RenderMode?.GetType() == other.RenderMode?.GetType();
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, RenderMode?.GetType());
    }

    /// <summary>
    /// Defines the types of web persistence scenarios.
    /// </summary>
    internal enum ScenarioType
    {
        /// <summary>
        /// State restoration during prerendering.
        /// </summary>
        Prerendering,

        /// <summary>
        /// State restoration during enhanced navigation.
        /// </summary>
        EnhancedNavigation,

        /// <summary>
        /// State restoration after server reconnection.
        /// </summary>
        Reconnection
    }
}