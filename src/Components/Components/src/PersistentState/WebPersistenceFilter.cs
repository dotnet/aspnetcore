// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides predefined filters for web-based persistent component state restoration scenarios.
/// </summary>
public sealed class WebPersistenceFilter : IPersistentStateFilter
{
    private readonly bool _enableForPrerendering;

    private WebPersistenceFilter(bool enabled)
    {
        _enableForPrerendering = enabled;
    }

    /// <summary>
    /// Gets a filter that enables state restoration during prerendering scenarios.
    /// </summary>
    public static WebPersistenceFilter Prerendering { get; } = new(enabled: true);

    /// <summary>
    /// Gets a filter that enables state restoration during prerendering scenarios.
    /// </summary>
    public static WebPersistenceFilter Reconnection { get; } = new(enabled: true);

    /// <summary>
    /// Determines whether this filter supports the specified scenario.
    /// </summary>
    /// <param name="scenario">The scenario to check.</param>
    /// <returns><see langword="true"/> if the filter supports the scenario; otherwise, <see langword="false"/>.</returns>
    public bool SupportsScenario(IPersistentComponentStateScenario scenario)
        => scenario is WebPersistenceScenario;

    /// <summary>
    /// Determines whether state should be restored for the specified scenario.
    /// This method is only called if <see cref="SupportsScenario"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="scenario">The scenario for which state restoration is being considered.</param>
    /// <returns><see langword="true"/> if state should be restored; otherwise, <see langword="false"/>.</returns>
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return _enableForPrerendering;
    }
}
