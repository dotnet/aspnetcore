// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a subscription to state restoration notifications.
/// </summary>
public readonly struct RestoringComponentStateSubscription : IDisposable
{
    private readonly List<(IPersistentComponentStateScenario Scenario, Action Callback, bool IsRecurring)>? _callbacks;
    private readonly IPersistentComponentStateScenario? _scenario;
    private readonly Action? _callback;
    private readonly bool _isRecurring;

    internal RestoringComponentStateSubscription(
        List<(IPersistentComponentStateScenario Scenario, Action Callback, bool IsRecurring)> callbacks,
        IPersistentComponentStateScenario scenario,
        Action callback,
        bool isRecurring)
    {
        _callbacks = callbacks;
        _scenario = scenario;
        _callback = callback;
        _isRecurring = isRecurring;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_callbacks != null && _scenario != null && _callback != null)
        {
            for (int i = _callbacks.Count - 1; i >= 0; i--)
            {
                var (scenario, callback, isRecurring) = _callbacks[i];
                if (ReferenceEquals(scenario, _scenario) && ReferenceEquals(callback, _callback) && isRecurring == _isRecurring)
                {
                    _callbacks.RemoveAt(i);
                    break;
                }
            }
        }
    }
}