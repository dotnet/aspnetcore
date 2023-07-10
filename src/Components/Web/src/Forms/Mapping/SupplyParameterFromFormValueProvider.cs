// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

internal class SupplyParameterFromFormValueProvider : ICascadingValueSupplier
{
    private readonly FormMappingContext _mappingContext;
    private readonly IFormValueMapper? _formValueMapper;

    public FormMappingContext MappingContext => _mappingContext;

    public SupplyParameterFromFormValueProvider(IFormValueMapper? formValueMapper, NavigationManager navigation, FormMappingContext? parentContext, string thisName)
    {
        ArgumentNullException.ThrowIfNull(navigation);

        _formValueMapper = formValueMapper;
        Name = thisName;

        // MappingContextId: action parameter used to define the handler
        // Name: form name and context used to map
        // Cases:
        // 1) No name ("")
        // Name = "";
        // MappingContextId = "";
        // <form name="" action="" />
        // 2) Name provided
        // Name = "my-handler";
        // MappingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        // <form name="my-handler" action="relative/path?existing=value&handler=my-handler
        // 3) Parent has a name "parent-name"
        // Name = "parent-name.my-handler";
        // MappingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        var name = FormMappingContext.Combine(parentContext, thisName);
        var mappingId = string.IsNullOrEmpty(name) ? "" : GenerateMappingContextId(name);
        _mappingContext = new FormMappingContext(name, mappingId);

        string GenerateMappingContextId(string name)
        {
            var mappingId = navigation.ToBaseRelativePath(navigation.GetUriWithQueryParameter("handler", name));
            var hashIndex = mappingId.IndexOf('#');
            return hashIndex == -1 ? mappingId : new string(mappingId.AsSpan(0, hashIndex));
        }
    }

    public string Name { get; }

    bool ICascadingValueSupplier.IsFixed => true;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a FormMappingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(FormMappingContext))
        {
            return true;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueMapper is not null && parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            var (formName, valueType) = GetFormNameAndValueType(_mappingContext, parameterInfo);
            return _formValueMapper.CanMap(valueType, formName);
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
        var (formName, valueType) = GetFormNameAndValueType(mappingContext, parameterInfo);

        var parameterName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
        var handler = ((SupplyParameterFromFormAttribute)parameterInfo.Attribute).Handler;
        Action<string, FormattableString, string?> errorHandler = string.IsNullOrEmpty(handler) ?
            mappingContext.AddError :
            (name, message, value) => mappingContext.AddError(formName, parameterName, message, value);

        var context = new FormValueMappingContext(formName!, valueType, parameterName)
        {
            OnError = errorHandler,
            MapErrorToContainer = mappingContext.AttachParentValue
        };

        formValueMapper.Map(context);

        return context.Result;
    }

    private static (string FormName, Type ValueType) GetFormNameAndValueType(FormMappingContext? mappingContext, in CascadingParameterInfo parameterInfo)
    {
        var valueType = parameterInfo.PropertyType;
        var valueName = ((SupplyParameterFromFormAttribute)parameterInfo.Attribute).Handler;
        var formName = string.IsNullOrEmpty(valueName) ?
            (mappingContext?.Name) :
            FormMappingContext.Combine(mappingContext, valueName);

        return (formName!, valueType);
    }
}
