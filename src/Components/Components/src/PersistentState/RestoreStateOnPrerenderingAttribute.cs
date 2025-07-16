// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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
/// <remarks>
/// Initializes a new instance of the <see cref="RestoreStateOnPrerenderingAttribute"/> class.
/// </remarks>
/// <param name="enable">
/// <see langword="true"/> to enable state restoration during prerendering (default);
/// <see langword="false"/> to disable state restoration during prerendering.
/// </param>
[AttributeUsage(AttributeTargets.Property)]
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class RestoreStateOnPrerenderingAttribute(bool enable = true) : Attribute, IPersistentStateFilter
{
    internal WebPersistenceFilter Filter { get; } = enable ? WebPersistenceFilter.Prerendering : new WebPersistenceFilter(WebPersistenceScenarioType.Prerendering, enabled: false);

    bool IPersistentStateFilter.SupportsScenario(IPersistentComponentStateScenario scenario)
        => Filter.SupportsScenario(scenario);

    bool IPersistentStateFilter.ShouldRestore(IPersistentComponentStateScenario scenario)
        => Filter.ShouldRestore(scenario);

    private string GetDebuggerDisplay()
    {
        return $"RestoreStateOnPrerenderingAttribute: (Enabled: {enable})";
    }
}
