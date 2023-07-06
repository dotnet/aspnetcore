﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

// TODO: Make this internal by changing CascadingModelBinder so it doesn't inherit from it, but instead
// implements ICascadingValueSupplier by forwarding all calls to an instance of this
public class SupplyParameterFromFormValueProvider : ICascadingValueSupplier
{
    public bool IsFixed => true;

    private ModelBindingContext? _bindingContext;
    private IFormValueSupplier _formValueSupplier;

    protected internal ModelBindingContext? BindingContext => _bindingContext;

    public void UpdateBindingInformation(IFormValueSupplier formValueSupplier, NavigationManager navigation, ModelBindingContext? parentContext, string thisName)
    {
        _formValueSupplier = formValueSupplier;

        // BindingContextId: action parameter used to define the handler
        // Name: form name and context used to bind
        // Cases:
        // 1) No name ("")
        // Name = "";
        // BindingContextId = "";
        // <form name="" action="" />
        // 2) Name provided
        // Name = "my-handler";
        // BindingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        // <form name="my-handler" action="relative/path?existing=value&handler=my-handler
        // 3) Parent has a name "parent-name"
        // Name = "parent-name.my-handler";
        // BindingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        var name = ModelBindingContext.Combine(parentContext, thisName);
        var bindingId = string.IsNullOrEmpty(name) ? "" : GenerateBindingContextId(name);
        _bindingContext = new ModelBindingContext(name, bindingId);
        parentContext?.SetErrors(name, _bindingContext);

        string GenerateBindingContextId(string name)
        {
            var bindingId = navigation.ToBaseRelativePath(navigation.GetUriWithQueryParameter("handler", name));
            var hashIndex = bindingId.IndexOf('#');
            return hashIndex == -1 ? bindingId : new string(bindingId.AsSpan(0, hashIndex));
        }
    }

    bool ICascadingValueSupplier.CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a ModelBindingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(ModelBindingContext))
        {
            return true;
        }

        // We also supply values for [SupplyValueFromForm]
        if (parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            var (formName, valueType) = GetFormNameAndValueType(_bindingContext, parameterInfo);
            return _formValueSupplier.CanBind(valueType, formName);
        }

        return false;
    }

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();

    object? ICascadingValueSupplier.GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a ModelBindingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(ModelBindingContext))
        {
            return _bindingContext;
        }

        // We also supply values for [SupplyValueFromForm]
        if (parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            return GetFormPostValue(_bindingContext, parameterInfo);
        }

        throw new InvalidOperationException($"Received an unexpected attribute type {parameterInfo.Attribute.GetType()}");
    }

    internal object? GetFormPostValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        Debug.Assert(bindingContext != null);
        var (formName, valueType) = GetFormNameAndValueType(bindingContext, parameterInfo);

        var parameterName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
        var handler = ((SupplyParameterFromFormAttribute)parameterInfo.Attribute).Handler;
        Action<string, FormattableString, string?> errorHandler = string.IsNullOrEmpty(handler) ?
            bindingContext.AddError :
            (name, message, value) => bindingContext.AddError(formName, parameterName, message, value);

        var context = new FormValueSupplierContext(formName!, valueType, parameterName)
        {
            OnError = errorHandler,
            MapErrorToContainer = bindingContext.AttachParentValue
        };

        _formValueSupplier.Bind(context);

        return context.Result;
    }

    private static (string FormName, Type ValueType) GetFormNameAndValueType(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        var valueType = parameterInfo.PropertyType;
        var valueName = ((SupplyParameterFromFormAttribute)parameterInfo.Attribute).Handler;
        var formName = string.IsNullOrEmpty(valueName) ?
            (bindingContext?.Name) :
            ModelBindingContext.Combine(bindingContext, valueName);

        return (formName!, valueType);
    }
}
