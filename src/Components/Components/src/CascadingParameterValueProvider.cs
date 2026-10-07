// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

internal class CascadingParameterValueProvider<TAttribute> : ICascadingValueSupplier
    where TAttribute : CascadingParameterAttributeBase
{
    private readonly Dictionary<ComponentSubscriptionKey, CascadingParameterSubscription> _subscriptions = new();
    private readonly Func<ComponentState, TAttribute, CascadingParameterInfo, CascadingParameterSubscription> _subscribeFactory;

    public CascadingParameterValueProvider(Func<ComponentState, TAttribute, CascadingParameterInfo, CascadingParameterSubscription> subscribeFactory)
    {
        _subscribeFactory = subscribeFactory;
    }

    public bool IsFixed => false;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is TAttribute;

    public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
    {
        var subscriptionKey = new ComponentSubscriptionKey((ComponentState)key!, parameterInfo.PropertyName);
        if (_subscriptions.TryGetValue(subscriptionKey, out var subscription))
        {
            return subscription.GetCurrentValue();
        }
        return null;
    }

    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var key = new ComponentSubscriptionKey(subscriber, parameterInfo.PropertyName);
        _subscriptions[key] = _subscribeFactory(subscriber, (TAttribute)parameterInfo.Attribute, parameterInfo);
    }

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var key = new ComponentSubscriptionKey(subscriber, parameterInfo.PropertyName);
        if (_subscriptions.Remove(key, out var subscription))
        {
            subscription.Dispose();
        }
    }
}
