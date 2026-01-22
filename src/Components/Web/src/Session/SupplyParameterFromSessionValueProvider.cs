// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web;

internal class SupplyParameterFromSessionValueProvider : ICascadingValueSupplier
{
    private readonly ISessionValueMapper _sessionValueMapper;

    public SupplyParameterFromSessionValueProvider(ISessionValueMapper sessionValueMapper)
    {
        _sessionValueMapper = sessionValueMapper;
    }

    public bool IsFixed => false;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromSessionAttribute;

    public object? GetCurrentValue(object? key, in CascadingParameterInfo parameterInfo)
    {
        if (_sessionValueMapper is null)
        {
            return null;
        }

        var attribute = (SupplyParameterFromSessionAttribute)parameterInfo.Attribute;
        var sessionKey = attribute.Name ?? parameterInfo.PropertyName;

        return _sessionValueMapper.GetValue(sessionKey, parameterInfo.GetType());
    }

    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {

    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'",
        Justification = "Component properties are preserved through other means.")]
    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        var attribute = (SupplyParameterFromSessionAttribute)parameterInfo.Attribute;
        var sessionKey = attribute.Name ?? parameterInfo.PropertyName;

        var componentType = subscriber.Component.GetType();
        var propertyInfo = componentType.GetProperty(parameterInfo.PropertyName);
        if (propertyInfo is null)
        {
            return;
        }
        var value = propertyInfo.GetValue(subscriber.Component);
        _sessionValueMapper.SetValue(sessionKey, value);
    }
}
