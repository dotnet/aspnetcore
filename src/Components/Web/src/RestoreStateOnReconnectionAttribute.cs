// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Indicates that state should be restored after server reconnection.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RestoreStateOnReconnectionAttribute : Attribute, IPersistentStateFilter
{
    internal WebPersistenceFilter? WebPersistenceFilter { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="RestoreStateOnReconnectionAttribute"/>.
    /// </summary>
    /// <param name="restore">Whether to restore state after reconnection. Default is true.</param>
    public RestoreStateOnReconnectionAttribute(bool restore = true)
    {
        WebPersistenceFilter = restore ? Components.Web.WebPersistenceFilter.Reconnection : null;
    }

    /// <inheritdoc />
    public bool ShouldRestore(IPersistentComponentStateScenario scenario)
    {
        return WebPersistenceFilter?.ShouldRestore(scenario) ?? false;
    }
}