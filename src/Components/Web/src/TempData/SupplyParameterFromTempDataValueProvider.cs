// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web;

internal class SupplyParameterFromTempDataValueProvider : ICascadingValueSupplier
{
    private readonly ITempDataValueMapper _tempDataValueMapper;

    public SupplyParameterFromTempDataValueProvider(ITempDataValueMapper tempDataValueMapper)
    {
        _tempDataValueMapper = tempDataValueMapper;
    }

    public bool IsFixed => false;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromTempDataAttribute;

    public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
    {
        if (_tempDataValueMapper is null)
        {
            return null;
        }

        var attribute = (SupplyParameterFromTempDataAttribute)parameterInfo.Attribute;
        var tempDataKey = (attribute.Name ?? parameterInfo.PropertyName).ToLowerInvariant();
        return _tempDataValueMapper.GetValue(tempDataKey, parameterInfo.PropertyType);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'",
        Justification = "Component properties are preserved through other means.")]
    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var attribute = (SupplyParameterFromTempDataAttribute)parameterInfo.Attribute;
        var tempDataKey = (attribute.Name ?? parameterInfo.PropertyName).ToLowerInvariant();
        var propertyName = parameterInfo.PropertyName;
        var propertyInfo = subscriber.Component.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (propertyInfo is null)
        {
            return;
        }

        var component = subscriber.Component;
        _tempDataValueMapper.RegisterValueCallback(tempDataKey, () =>
        {
            var value = propertyInfo.GetValue(component);
            return value;
        });
    }

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
    }
}
