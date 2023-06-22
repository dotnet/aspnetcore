// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        => _formValueSupplier.CanConvertSingleValue(type);

    /// <inhertidoc/>
    protected internal override bool CanSupplyValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        var (formName, valueType) = GetFormNameAndValueType(bindingContext, parameterInfo);
        return _formValueSupplier.CanBind(formName!, valueType);
    }

    /// <inhertidoc/>
    protected internal override object? GetCurrentValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        var (formName, valueType) = GetFormNameAndValueType(bindingContext, parameterInfo);

        if (!_formValueSupplier.TryBind(formName!, valueType, out var boundValue))
        {
            // TODO: Report errors
            return null;
        }

        return boundValue;
    }

    private static (string FormName, Type ValueType) GetFormNameAndValueType(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        var valueType = parameterInfo.PropertyType;
        var valueName = parameterInfo.Attribute.Name;
        var formName = string.IsNullOrEmpty(valueName) ?
            (bindingContext?.Name) :
            ModelBindingContext.Combine(bindingContext, valueName);

        return (formName!, valueType);
    }
}
