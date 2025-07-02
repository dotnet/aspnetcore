// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Provides filters for web-based persistent component state restoration scenarios.
/// </summary>
public sealed class WebPersistenceFilter : IPersistentStateFilter
{
    /// <summary>
    /// Gets a filter that matches enhanced navigation scenarios.
    /// </summary>
    public static WebPersistenceFilter EnhancedNavigation { get; } = new(WebPersistenceScenario.ScenarioType.EnhancedNavigation);

    /// <summary>
    /// Gets a filter that matches prerendering scenarios.
    /// </summary>
    public static WebPersistenceFilter Prerendering { get; } = new(WebPersistenceScenario.ScenarioType.Prerendering);

    /// <summary>
    /// Gets a filter that matches reconnection scenarios.
    /// </summary>
    public static WebPersistenceFilter Reconnection { get; } = new(WebPersistenceScenario.ScenarioType.Reconnection);

    private readonly WebPersistenceScenario.ScenarioType _targetScenario;

    private WebPersistenceFilter(WebPersistenceScenario.ScenarioType targetScenario)
    {
        _targetScenario = targetScenario;
    }

    /// <inheritdoc />
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return scenario is WebPersistenceScenario webScenario && webScenario.Type == _targetScenario;
    }
}