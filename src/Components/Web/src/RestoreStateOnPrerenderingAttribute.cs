// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored during prerendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnPrerenderingAttribute : Attribute, IPersistentStateFilter
{
    internal WebPersistenceFilter WebPersistenceFilter { get; } = WebPersistenceFilter.Prerendering;

    /// <inheritdoc />
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return WebPersistenceFilter.ShouldRestore(scenario);
    }
}