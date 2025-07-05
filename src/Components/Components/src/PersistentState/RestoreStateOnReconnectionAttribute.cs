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
        Enabled = enabled;
    }

    /// <summary>
    /// Gets a value indicating whether state restoration is enabled during reconnection.
    /// </summary>
    internal bool Enabled { get; }

    /// <summary>
    /// Determines whether this filter supports the specified scenario.
    /// </summary>
    /// <param name="scenario">The scenario to check.</param>
    /// <returns><see langword="true"/> if the filter supports the scenario; otherwise, <see langword="false"/>.</returns>
    public bool SupportsScenario(IPersistentComponentStateScenario scenario)
        => scenario == WebPersistenceScenario.Reconnection;

    /// <summary>
    /// Determines whether state should be restored for the specified scenario.
    /// This method is only called if <see cref="SupportsScenario"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="scenario">The scenario for which state restoration is being considered.</param>
    /// <returns><see langword="true"/> if state should be restored; otherwise, <see langword="false"/>.</returns>
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        if (scenario == WebPersistenceScenario.Reconnection)
        {
            return Enabled;
        }
        // For other scenarios, use default behavior (restore)
        return true;
    }
}
