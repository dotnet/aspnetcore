// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Defines filtering logic for persistent component state restoration.
/// </summary>
public interface IPersistentStateFilter
{
    /// <summary>
    /// Determines whether state should be restored for the given scenario.
    /// </summary>
    /// <param name="scenario">The restoration scenario.</param>
    /// <returns>True if state should be restored; otherwise false.</returns>
    bool ShouldRestore(IPersistentComponentStateScenario scenario);
}