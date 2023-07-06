// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Enables component parameters to be supplied from the query string with <see cref="SupplyParameterFromFormAttribute"/>.
/// </summary>
public sealed class CascadingFormModelBindingProvider : CascadingModelBindingProvider
{
    private readonly IFormValueSupplier _formValueSupplier;

    /// <inheritdoc/>
    protected internal override bool AreValuesFixed => true;

    /// <summary>
    /// Constructs a new instance of <see cref="CascadingFormModelBindingProvider"/>.
    /// </summary>
    /// <param name="formValueSupplier">The <see cref="IFormValueSupplier"/>.</param>
    public CascadingFormModelBindingProvider(IFormValueSupplier formValueSupplier)
    {
        _formValueSupplier = formValueSupplier;
    }

    /// <inhertidoc/>
    protected internal override bool SupportsCascadingParameterAttributeType(Type attributeType)
        => attributeType == typeof(SupplyParameterFromFormAttribute);

    /// <inhertidoc/>
    protected internal override bool SupportsParameterType(Type type)
        => _formValueSupplier.CanBind(type, null);

    /// <inhertidoc/>
    protected internal override bool CanSupplyValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        var (formName, valueType) = GetFormNameAndValueType(bindingContext, parameterInfo);
        return _formValueSupplier.CanBind(valueType, formName);
    }

    /// <inhertidoc/>
    protected internal override object? GetCurrentValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
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
