// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides filtering logic for persistent component state restoration based on scenarios.
/// </summary>
public interface IPersistentStateFilter
{
    /// <summary>
    /// Determines whether this filter supports the specified scenario.
    /// </summary>
    /// <param name="scenario">The scenario to check.</param>
    /// <returns><see langword="true"/> if the filter supports the scenario; otherwise, <see langword="false"/>.</returns>
    bool SupportsScenario(IPersistentComponentStateScenario scenario);

    /// <summary>
    /// Determines whether state should be restored for the specified scenario.
    /// This method is only called if <see cref="SupportsScenario"/> returns <see langword="true"/>.
    /// </summary>
    /// <param name="scenario">The scenario for which state restoration is being considered.</param>
    /// <returns><see langword="true"/> if state should be restored; otherwise, <see langword="false"/>.</returns>
    bool ShouldRestore(IPersistentComponentStateScenario scenario);
}
