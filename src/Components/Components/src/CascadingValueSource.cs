// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Supplies a cascading value that can be received by components using
/// <see cref="CascadingParameterAttribute"/>.
/// </summary>
public class CascadingValueSource<TValue> : ICascadingValueSupplier
{
    // By *not* making this sealed, people who want to deal with value disposal can subclass this,
    // add IDisposable, and then do what they want during shutdown

    private readonly ConcurrentDictionary<Dispatcher, List<ComponentState>>? _subscribers;
    private readonly bool _isFixed;
    private readonly string? _name;

    /// <summary>
    /// Gets the current value.
    /// </summary>
    protected TValue CurrentValue { get; private set; }

    /// <summary>
    /// Constructs an instance of <see cref="CascadingValueSource{TValue}"/>.
    /// </summary>
    /// <param name="value">The initial value.</param>
    /// <param name="isFixed">A flag to indicate whether the value is fixed. If false, all receipients will subscribe for update notifications, which you can issue by calling <see cref="NotifyChangedAsync()"/>. These subscriptions come at a performance cost, so if the value will not change, set <paramref name="isFixed"/> to true.</param>
    public CascadingValueSource(TValue value, bool isFixed)
    {
        CurrentValue = value;
        _isFixed = isFixed;

        if (!_isFixed)
        {
            _subscribers = new();
        }
    }

    /// <summary>
    /// Constructs an instance of <see cref="CascadingValueSource{TValue}"/>.
    /// </summary>
    /// <param name="name">A name for the cascading value. If set, <see cref="CascadingParameterAttribute"/> can be configured to match based on this name.</param>
    /// <param name="value">The initial value.</param>
    /// <param name="isFixed">A flag to indicate whether the value is fixed. If false, all receipients will subscribe for update notifications, which you can issue by calling <see cref="NotifyChangedAsync()"/>. These subscriptions come at a performance cost, so if the value will not change, set <paramref name="isFixed"/> to true.</param>
    public CascadingValueSource(string name, TValue value, bool isFixed) : this(value, isFixed)
    {
        _name = name;
    }

    /// <summary>
    /// Notifies subscribers that the value has changed (for example, if it has been mutated).
    /// </summary>
    /// <returns>A <see cref="Task"/> that completes when the notifications have been issued.</returns>
    public Task NotifyChangedAsync()
    {
        if (_isFixed)
        {
            throw new InvalidOperationException($"Cannot notify about changes because the {GetType()} is configured as fixed.");
        }

        if (_subscribers?.Count > 0)
        {
            var tasks = new List<Task>();

            foreach (var (dispatcher, subscribers) in _subscribers)
            {
                tasks.Add(dispatcher.InvokeAsync(() =>
                {
                    foreach (var subscriber in subscribers)
                    {
                        subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
                    }
                }));
            }

            return Task.WhenAll(tasks);
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Notifies subscribers that the value has changed, supplying a new value.
    /// This changes the value of <see cref="CurrentValue"/>.
    /// </summary>
    /// <param name="newValue"></param>
    /// <returns>A <see cref="Task"/> that completes when the notifications have been issued.</returns>
    public Task NotifyChangedAsync(TValue newValue)
    {
        CurrentValue = newValue;
        return NotifyChangedAsync();
    }

    bool ICascadingValueSupplier.IsFixed => _isFixed;

    bool ICascadingValueSupplier.CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        if (parameterInfo.Attribute is not CascadingParameterAttribute cascadingParameterAttribute || !parameterInfo.PropertyType.IsAssignableFrom(typeof(TValue)))
        {
            return false;
        }

        // We only consider explicitly requested names, not the property name.
        var requestedName = cascadingParameterAttribute.Name;
        return (requestedName == null && _name == null) // Match on type alone
            || string.Equals(requestedName, _name, StringComparison.OrdinalIgnoreCase); // Also match on name
    }

    object? ICascadingValueSupplier.GetCurrentValue(in CascadingParameterInfo parameterInfo)
        => CurrentValue;

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        Dispatcher dispatcher = subscriber.Renderer.Dispatcher;
        dispatcher.AssertAccess();

        // The .Add is threadsafe because we are in the sync context for this dispatcher
        _subscribers?.GetOrAdd(dispatcher, _ => new()).Add(subscriber);
    }

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        Dispatcher dispatcher = subscriber.Renderer.Dispatcher;
        dispatcher.AssertAccess();

        if (_subscribers?.TryGetValue(dispatcher, out var subscribersForDispatcher) == true)
        {
            // Threadsafe because we're in the sync context for this dispatcher
            subscribersForDispatcher.Remove(subscriber);
            if (subscribersForDispatcher.Count == 0)
            {
                _subscribers.Remove(dispatcher, out _);
            }
        }
    }
}
