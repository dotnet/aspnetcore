// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a scenario for persistent component state restoration.
/// </summary>
public interface IPersistentComponentStateScenario
{
    /// <summary>
    /// Gets a value indicating whether this scenario can occur multiple times during the component's lifetime.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the scenario can occur multiple times (e.g., enhanced navigation);
    /// <see langword="false"/> if the scenario occurs only once (e.g., prerendering).
    /// </value>
    bool IsRecurring { get; }
}
