// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Controls whether state should be restored for the decorated property during reconnection scenarios.
/// This attribute can be applied to properties marked with <see cref="PersistentStateAttribute"/>.
/// </summary>
/// <example>
/// <code>
/// [Parameter]
/// [PersistState]
/// [RestoreStateOnReconnection]
/// public int Counter { get; set; }
///
/// [Parameter]
/// [PersistState]
/// [RestoreStateOnReconnection(false)]
/// public int NonPersistedCounter { get; set; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnReconnectionAttribute : Attribute, IPersistentStateFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreStateOnReconnectionAttribute"/> class.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to enable state restoration during reconnection (default);
    /// <see langword="false"/> to disable state restoration during reconnection.
    /// </param>
    public RestoreStateOnReconnectionAttribute(bool enabled = true)
    {
        Filter = enabled ? WebPersistenceFilter.Reconnection : new WebPersistenceFilter(WebPersistenceScenarioType.Reconnection, enabled: false);
    }

    internal WebPersistenceFilter Filter { get; }

    bool IPersistentStateFilter.SupportsScenario(IPersistentComponentStateScenario scenario)
        => Filter.SupportsScenario(scenario);

    bool IPersistentStateFilter.ShouldRestore(IPersistentComponentStateScenario scenario)
        => Filter.ShouldRestore(scenario);
}
