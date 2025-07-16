// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides predefined filters for web-based persistent component state restoration scenarios.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class WebPersistenceFilter : IPersistentStateFilter
{
    private readonly WebPersistenceScenarioType _scenarioType;
    private readonly bool _enabled;

    internal WebPersistenceFilter(WebPersistenceScenarioType scenarioType, bool enabled)
    {
        _scenarioType = scenarioType;
        _enabled = enabled;
    }

    /// <summary>
    /// Gets a filter that enables state restoration during prerendering scenarios.
    /// </summary>
    public static WebPersistenceFilter Prerendering { get; } = new(WebPersistenceScenarioType.Prerendering, enabled: true);

    /// <summary>
    /// Gets a filter that enables state restoration during reconnection scenarios.
    /// </summary>
    /// <example>
    /// <code>
    /// if (filter.ShouldRestore(WebPersistenceScenario.Reconnection))
    /// {
    ///     // Restore state for reconnection
    /// }
    /// </code>
    /// </example>
    public static WebPersistenceFilter Reconnection { get; } = new(WebPersistenceScenarioType.Reconnection, enabled: true);

    /// <summary>
    /// Gets a filter that enables state restoration during enhanced navigation scenarios.
    /// </summary>
    public static WebPersistenceFilter EnhancedNavigation { get; } = new(WebPersistenceScenarioType.EnhancedNavigation, enabled: true);

    /// <summary>
    /// Determines whether this filter supports the specified scenario.
    /// </summary>
    /// <param name="scenario">The scenario to check.</param>
    /// <returns><see langword="true"/> if the filter supports the scenario; otherwise, <see langword="false"/>.</returns>
    public bool SupportsScenario(IPersistentComponentStateScenario scenario)
        => scenario is WebPersistenceScenario webScenario && webScenario.ScenarioType == _scenarioType;

    /// <summary>
    /// Determines whether state should be restored for the specified scenario.
    /// This method is only called if <see cref="SupportsScenario"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="scenario">The scenario for which state restoration is being considered.</param>
    /// <returns><see langword="true"/> if state should be restored; otherwise, <see langword="false"/>.</returns>
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return _enabled;
    }

    private string GetDebuggerDisplay() => $"{_scenarioType} (Enabled: {_enabled})";
}
