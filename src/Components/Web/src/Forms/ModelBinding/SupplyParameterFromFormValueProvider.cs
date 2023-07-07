// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms.ModelBinding;

internal class SupplyParameterFromFormValueProvider : ICascadingValueSupplier
{
    private readonly ModelBindingContext _bindingContext;
    private readonly IFormValueModelBinder? _formValueModelBinder;

    public ModelBindingContext BindingContext => _bindingContext;

    public SupplyParameterFromFormValueProvider(IFormValueModelBinder? formValueModelBinder, NavigationManager navigation, ModelBindingContext? parentContext, string thisName)
    {
        ArgumentNullException.ThrowIfNull(navigation);

        _formValueModelBinder = formValueModelBinder;
        Name = thisName;

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

        string GenerateBindingContextId(string name)
        {
            var bindingId = navigation.ToBaseRelativePath(navigation.GetUriWithQueryParameter("handler", name));
            var hashIndex = bindingId.IndexOf('#');
            return hashIndex == -1 ? bindingId : new string(bindingId.AsSpan(0, hashIndex));
        }
    }

    public string Name { get; }

    bool ICascadingValueSupplier.IsFixed => true;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a ModelBindingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(ModelBindingContext))
        {
            return true;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueModelBinder is not null && parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            var (formName, valueType) = GetFormNameAndValueType(_bindingContext, parameterInfo);
            return _formValueModelBinder.CanBind(valueType, formName);
        }

        return false;
    }

    public object? GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a ModelBindingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(ModelBindingContext))
        {
            return _bindingContext;
        }

        // We also supply values for [SupplyValueFromForm]
        if (_formValueModelBinder is { } binder && parameterInfo.Attribute is SupplyParameterFromFormAttribute)
        {
            return GetFormPostValue(binder, _bindingContext, parameterInfo);
        }

        throw new InvalidOperationException($"Received an unexpected attribute type {parameterInfo.Attribute.GetType()}");
    }

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException(); // IsFixed = true, so the framework won't call this

    internal static object? GetFormPostValue(IFormValueModelBinder formValueModelBinder, ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        Debug.Assert(bindingContext != null);
        var (formName, valueType) = GetFormNameAndValueType(bindingContext, parameterInfo);

        var parameterName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
        var handler = ((SupplyParameterFromFormAttribute)parameterInfo.Attribute).Handler;
        Action<string, FormattableString, string?> errorHandler = string.IsNullOrEmpty(handler) ?
            bindingContext.AddError :
            (name, message, value) => bindingContext.AddError(formName, parameterName, message, value);

        var context = new FormValueModelBindingContext(formName!, valueType, parameterName)
        {
            OnError = errorHandler,
            MapErrorToContainer = bindingContext.AttachParentValue
        };

        formValueModelBinder.Bind(context);

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
