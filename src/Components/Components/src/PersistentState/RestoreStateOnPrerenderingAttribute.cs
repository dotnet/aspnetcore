// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Controls whether state should be restored for the decorated property during prerendering scenarios.
/// This attribute can be applied to properties marked with <see cref="PersistentStateAttribute"/>.
/// </summary>
/// <example>
/// <code>
/// [Parameter]
/// [PersistState]
/// [RestoreStateOnPrerendering]
/// public string? UserName { get; set; }
///
/// [Parameter]
/// [PersistState]
/// [RestoreStateOnPrerendering(false)]
/// public string? NonPrerenderingData { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnPrerenderingAttribute : Attribute, IPersistentStateFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreStateOnPrerenderingAttribute"/> class.
    /// </summary>
    /// <param name="enable">
    /// <see langword="true"/> to enable state restoration during prerendering (default);
    /// <see langword="false"/> to disable state restoration during prerendering.
    /// </param>
    public RestoreStateOnPrerenderingAttribute(bool enable = true)
    {
        Filter = enable ? WebPersistenceFilter.Prerendering : new WebPersistenceFilter(WebPersistenceScenarioType.Prerendering, enabled: false);
    }

    internal WebPersistenceFilter Filter { get; }

    bool IPersistentStateFilter.SupportsScenario(IPersistentComponentStateScenario scenario)
        => Filter.SupportsScenario(scenario);

    bool IPersistentStateFilter.ShouldRestore(IPersistentComponentStateScenario scenario)
        => Filter.ShouldRestore(scenario);
}
