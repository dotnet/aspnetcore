// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored during prerendering.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnPrerenderingAttribute : Attribute, IPersistentStateFilter
{
    internal WebPersistenceFilter? WebPersistenceFilter { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RestoreStateOnPrerenderingAttribute"/>.
    /// </summary>
    /// <param name="restore">Whether to restore state during prerendering. Default is true.</param>
    public RestoreStateOnPrerenderingAttribute(bool restore = true)
    {
        WebPersistenceFilter = restore ? Components.Web.WebPersistenceFilter.Prerendering : null;
    }

    /// <inheritdoc />
    bool IPersistentStateFilter.ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return WebPersistenceFilter?.ShouldRestore(scenario) ?? false;
    }
}