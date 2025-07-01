// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a scenario for persistent component state restoration.
/// </summary>
public interface IPersistentComponentStateScenario
{
    /// <summary>
    /// Gets a value indicating whether callbacks for this scenario can be invoked multiple times.
    /// If false, callbacks are automatically unregistered after first invocation.
    /// </summary>
    bool IsRecurring { get; }
}