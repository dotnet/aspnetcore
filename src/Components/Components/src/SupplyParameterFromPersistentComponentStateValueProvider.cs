// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components;

internal sealed class SupplyParameterFromPersistentComponentStateValueProvider(PersistentComponentState state) : ICascadingValueSupplier
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyGetter> _propertyGetterCache = new();

    private readonly Dictionary<ComponentState, PersistingComponentStateSubscription> _subscriptions = [];

    public bool IsFixed => false;
    // For testing purposes only
    internal Dictionary<ComponentState, PersistingComponentStateSubscription> Subscriptions => _subscriptions;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromPersistentComponentStateAttribute;

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
        var storageKey = componentState.ComputeKey(parameterInfo.PropertyName);

        return state.TryTakeFromJson(storageKey, parameterInfo.PropertyType, out var value) ? value : null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")] [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "OpenComponent already has the right set of attributes")] [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "OpenComponent already has the right set of attributes")]
    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var propertyName = parameterInfo.PropertyName;
        var propertyType = parameterInfo.PropertyType;
        _subscriptions[subscriber] = state.RegisterOnPersisting(() =>
            {
                var storageKey = subscriber.ComputeKey(propertyName);
                var propertyGetter = ResolvePropertyGetter(subscriber.Component.GetType(), propertyName);
                var property = propertyGetter.GetValue(subscriber.Component);
                if (property == null)
                {
                    return Task.CompletedTask;
                }
                state.PersistAsJson(storageKey, property, propertyType);
                return Task.CompletedTask;
            }, subscriber.Renderer.GetComponentRenderMode(subscriber.Component));
    }

    private static PropertyGetter ResolvePropertyGetter(Type type, string propertyName)
    {
        return _propertyGetterCache.GetOrAdd((type, propertyName), PropertyGetterFactory);
    }

    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2077:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The source field does not have matching annotations.",
    Justification = "Properties of rendered components are preserved through other means and won't get trimmed.")]

    private static PropertyGetter PropertyGetterFactory((Type type, string propertyName) key)
    {
        var (type, propertyName) = key;
        var propertyInfo = GetPropertyInfo(type, propertyName);
        if (propertyInfo == null)
        {
            throw new InvalidOperationException($"Property {propertyName} not found on type {type.FullName}");
        }
        return new PropertyGetter(type, propertyInfo);

        static PropertyInfo? GetPropertyInfo([DynamicallyAccessedMembers(LinkerFlags.Component)] Type type, string propertyName)
            => type.GetProperty(propertyName);
    }

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        if (_subscriptions.TryGetValue(subscriber, out var subscription))
        {
            subscription.Dispose();
            _subscriptions.Remove(subscriber);
        }
    }
}
