// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored during enhanced navigation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UpdateStateOnEnhancedNavigationAttribute : Attribute, IPersistentStateFilter
{
    internal WebPersistenceFilter WebPersistenceFilter { get; } = WebPersistenceFilter.EnhancedNavigation;

    /// <inheritdoc />
    bool IPersistentStateFilter.ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return WebPersistenceFilter.ShouldRestore(scenario);
    }
}