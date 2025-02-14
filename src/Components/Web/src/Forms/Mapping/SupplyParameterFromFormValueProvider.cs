// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

// Provides values for [SupplyParameterFromForm] parameters on components.
// It is used in two ways:
// - By default, an instance is registered in DI, supplying values outside any FormMappingScope
// - If there is a FormMappingScope, internally it creates an instance of this to implement ICascadingValueSupplier for it
internal class SupplyParameterFromFormValueProvider : ICascadingValueSupplier
{
    private readonly IFormValueMapper? _formValueMapper;
    private readonly FormMappingContext _mappingContext;

    public FormMappingContext MappingContext => _mappingContext;

    public SupplyParameterFromFormValueProvider(IFormValueMapper? formValueMapper, string mappingScopeName)
    {
        _formValueMapper = formValueMapper;
        _mappingContext = new FormMappingContext(mappingScopeName);

        MappingScopeName = mappingScopeName;
    }

    public string MappingScopeName { get; }

    bool ICascadingValueSupplier.IsFixed => true;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a FormMappingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(FormMappingContext))
        {
            return true;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueMapper is not null && parameterInfo.Attribute is SupplyParameterFromFormAttribute supplyParameterFromFormAttribute)
        {
            return _formValueMapper.CanMap(parameterInfo.PropertyType, MappingScopeName, supplyParameterFromFormAttribute.FormName);
        }

        return false;
    }

    public object? GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a FormMappingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(FormMappingContext))
        {
            return _mappingContext;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueMapper is { } valueMapper && parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            return GetFormPostValue(valueMapper, _mappingContext, parameterInfo);
        }

        throw new InvalidOperationException($"Received an unexpected attribute type {parameterInfo.Attribute.GetType()}");
    }

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    internal static object? GetFormPostValue(IFormValueMapper formValueMapper, FormMappingContext? mappingContext, in CascadingParameterInfo parameterInfo)
    {
        Debug.Assert(mappingContext != null);

        var attribute = (SupplyParameterFromFormAttribute)parameterInfo.Attribute; // Must be a valid cast because we check in CanSupplyValue
        var parameterName = attribute.Name ?? parameterInfo.PropertyName;
        var restrictToFormName = attribute.FormName;
        Action<string, FormattableString, string?> errorHandler = string.IsNullOrEmpty(restrictToFormName) ?
            mappingContext.AddError :
            (name, message, value) => mappingContext.AddError(restrictToFormName, parameterName, message, value);

        var context = new FormValueMappingContext(mappingContext.MappingScopeName, restrictToFormName, parameterInfo.PropertyType, parameterName)
        {
            OnError = errorHandler,
            MapErrorToContainer = mappingContext.AttachParentValue
        };

        formValueMapper.Map(context);

        return context.Result;
    }
}
