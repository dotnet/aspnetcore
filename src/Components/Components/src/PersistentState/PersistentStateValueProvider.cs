// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal sealed partial class PersistentStateValueProvider(PersistentComponentState state, ILogger<PersistentStateValueProvider> logger, IServiceProvider serviceProvider) : ICascadingValueSupplier
{
    private readonly Dictionary<ComponentSubscriptionKey, PersistentValueProviderComponentSubscription> _subscriptions = [];

    public bool IsFixed => false;
    // For testing purposes only
    internal Dictionary<ComponentSubscriptionKey, PersistentValueProviderComponentSubscription> Subscriptions => _subscriptions;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is PersistentStateAttribute;

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2026:RequiresUnreferencedCode message",
        Justification = "JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
    {
        var componentState = (ComponentState)key!;

        if (_subscriptions.TryGetValue(new(componentState, parameterInfo.PropertyName), out var subscription))
        {
            return subscription.GetOrComputeLastValue();
        }

        return null;
    }

    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => Subscribe(subscriber, parameterInfo, preserveCurrentValue: false);

    void ICascadingValueSupplier.Subscribe(
        ComponentState subscriber,
        in CascadingParameterInfo parameterInfo,
        CascadingParameterSubscriptionMode mode)
        => Subscribe(subscriber, parameterInfo, preserveCurrentValue: mode is CascadingParameterSubscriptionMode.MetadataRefresh);

    private void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo, bool preserveCurrentValue)
    {
        var propertyName = parameterInfo.PropertyName;

        var componentSubscription = new PersistentValueProviderComponentSubscription(
            state,
            subscriber,
            parameterInfo,
            serviceProvider,
            logger);

        if (preserveCurrentValue)
        {
            // Registering can queue an initial restore synchronously. A retained component has already
            // completed that phase, so initialize the replacement subscription from its current value.
            componentSubscription.PreserveCurrentValue();
        }

        _subscriptions.Add(new ComponentSubscriptionKey(subscriber, propertyName), componentSubscription);
    }

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        if (_subscriptions.TryGetValue(new(subscriber, parameterInfo.PropertyName), out var subscription))
        {
            subscription.Dispose();
            _subscriptions.Remove(new(subscriber, parameterInfo.PropertyName));
        }
    }
}
