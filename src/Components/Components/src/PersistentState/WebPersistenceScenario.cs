// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

internal enum WebPersistenceScenarioType
{
    Prerendering,
    Reconnection,
    EnhancedNavigation
}

/// <summary>
/// Provides predefined scenarios for web-based persistent component state restoration.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class WebPersistenceScenario : IPersistentComponentStateScenario
{
    private WebPersistenceScenario(WebPersistenceScenarioType scenarioType, bool isRecurring)
    {
        IsRecurring = isRecurring;
        ScenarioType = scenarioType;
    }

    internal WebPersistenceScenarioType ScenarioType { get; }

    /// <summary>
    /// Gets a value indicating whether this scenario can occur multiple times during the component's lifetime.
    /// </summary>
    public bool IsRecurring { get; }

    /// <summary>
    /// Gets a scenario representing prerendering state restoration.
    /// This scenario occurs once when components are initially rendered on the server before interactivity.
    /// </summary>
    public static WebPersistenceScenario Prerendering { get; } = new(WebPersistenceScenarioType.Prerendering, isRecurring: false);

    /// <summary>
    /// Gets a scenario representing reconnection state restoration.
    /// This scenario occurs once when a server circuit is evicted and later restored after a client reconnection.
    /// </summary>
    /// <example>
    /// <code>
    /// if (scenario == WebPersistenceScenario.Reconnection)
    /// {
    ///     // Handle reconnection-specific state restoration
    /// }
    /// </code>
    /// </example>
    public static WebPersistenceScenario Reconnection { get; } = new(WebPersistenceScenarioType.Reconnection, isRecurring: false);

    /// <summary>
    /// Gets a scenario representing enhanced navigation state restoration.
    /// This scenario can occur multiple times during enhanced page navigation.
    /// </summary>
    public static WebPersistenceScenario EnhancedNavigation { get; } = new(WebPersistenceScenarioType.EnhancedNavigation, isRecurring: true);

    private string GetDebuggerDisplay() => $"{ScenarioType} (IsRecurring: {IsRecurring})";
}
