// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides predefined scenarios for web-based persistent component state restoration.
/// </summary>
public sealed class WebPersistenceScenario : IPersistentComponentStateScenario
{
    private WebPersistenceScenario(bool isRecurring)
    {
        IsRecurring = isRecurring;
    }

    /// <summary>
    /// Gets a value indicating whether this scenario can occur multiple times during the component's lifetime.
    /// </summary>
    public bool IsRecurring { get; }

    /// <summary>
    /// Gets a scenario representing prerendering state restoration.
    /// This scenario occurs once when components are initially rendered on the server before interactivity.
    /// </summary>
    public static WebPersistenceScenario Prerendering { get; } = new(isRecurring: false);
}
