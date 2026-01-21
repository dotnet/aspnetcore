// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web;

internal class SupplyParameterFromSessionValueProvider : ICascadingValueSupplier
{
    private readonly ISessionValueMapper _sessionValueMapper;

    public SupplyParameterFromSessionValueProvider(ISessionValueMapper sessionValueMapper)
    {
        _sessionValueMapper = sessionValueMapper;
    }

    public bool IsFixed => true;

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

        return _sessionValueMapper.GetValue(sessionKey);
    }

    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this
}
