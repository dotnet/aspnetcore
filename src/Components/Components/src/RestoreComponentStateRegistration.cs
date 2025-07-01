// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a registration for state restoration callbacks.
/// </summary>
internal readonly struct RestoreComponentStateRegistration
{
    public RestoreComponentStateRegistration(IPersistentComponentStateScenario scenario, Action callback)
    {
        Scenario = scenario;
        Callback = callback;
    }

    public IPersistentComponentStateScenario Scenario { get; }
    public Action Callback { get; }
}