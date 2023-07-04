// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// 
/// </summary>
public class CascadingValueSource<TValue> : ICascadingValueSupplier
{
    // By *not* making this sealed, people who want to deal with value disposal can subclass this,
    // add IDisposable, and then do what they want during shutdown

    // TODO: Another approach to consider is simply imposing the rule that a given CascadingValueSource
    //       is affinitized to the first Dispatcher it sees, and throws if you try to subscribe to it from
    //       a ComponentState with a different Dispatcher. But is it possible to have multiple renderers
    //       within a single DI scope? Even if it isn't how typical Blazor apps are set up, it's best not
    //       to impose extra rules that could affect other hosting models.
    private ConcurrentDictionary<Dispatcher, List<ComponentState>>? _subscribers; // Lazily instantiated

    private readonly bool _isFixed;
    private readonly string? _name;

    /// <summary>
    /// 
    /// </summary>
    protected TValue CurrentValue { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="isFixed"></param>
    public CascadingValueSource(TValue value, bool isFixed)
    {
        CurrentValue = value;
        _isFixed = isFixed;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="name"></param>
    /// <param name="isFixed"></param>
    public CascadingValueSource(string name, TValue value, bool isFixed) : this(value, isFixed)
    {
        _name = name;
    }

    /// <summary>
    /// 
    /// </summary>
    public Task NotifyChangedAsync()
    {
        if (_isFixed)
        {
            throw new InvalidOperationException($"Cannot notify about changes because the {GetType()} is configured as fixed.");
        }

        if (_subscribers is not null)
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
    /// 
    /// </summary>
    /// <param name="newValue"></param>
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

        _subscribers ??= new();
        _subscribers.GetOrAdd(dispatcher, _ => new()).Add(subscriber); // The .Add is threadsafe because we are in the sync context for this dispatcher
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
