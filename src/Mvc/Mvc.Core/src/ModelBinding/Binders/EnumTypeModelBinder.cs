// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// <see cref="IModelBinder"/> implementation to bind models for types deriving from <see cref="Enum"/>.
/// </summary>
public class EnumTypeModelBinder : SimpleTypeModelBinder
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeModelBinder"/>.
    /// </summary>
    /// <param name="suppressBindingUndefinedValueToEnumType">
    /// Flag to determine if binding to undefined should be suppressed or not.
    /// </param>
    /// <param name="modelType">The model type.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>,</param>
    /// <remarks>
    /// The <paramref name="suppressBindingUndefinedValueToEnumType"/> parameter is currently ignored.
    /// </remarks>
    public EnumTypeModelBinder(
        bool suppressBindingUndefinedValueToEnumType,
        Type modelType,
        ILoggerFactory loggerFactory)
        : base(modelType, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void CheckModel(
        ModelBindingContext bindingContext,
        ValueProviderResult valueProviderResult,
        object? model)
    {
        if (model == null)
        {
            base.CheckModel(bindingContext, valueProviderResult, model);
        }
        else if (IsDefinedInEnum(model, bindingContext))
        {
            bindingContext.Result = ModelBindingResult.Success(model);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueIsInvalidAccessor(
                    valueProviderResult.ToString()));
        }
    }

    private static bool IsDefinedInEnum(object model, ModelBindingContext bindingContext)
    {
        var modelType = bindingContext.ModelMetadata.UnderlyingOrModelType;

        // Check if the converted value is indeed defined on the enum as EnumTypeConverter
        // converts value to the backing type (ex: integer) and does not check if the value is defined on the enum.
        if (bindingContext.ModelMetadata.IsFlagsEnum)
        {
            // Enum.IsDefined does not work with combined flag enum values.
            // From EnumDataTypeAttribute.cs in CoreFX.
            // Examples:
            //
            // [Flags]
            // enum FlagsEnum { Value1 = 1, Value2 = 2, Value4 = 4 }
            //
            // Valid Scenarios:
            // 1. valueProviderResult="Value2,Value4", model=Value2 | Value4, underlying=6, converted=Value2, Value4
            // 2. valueProviderResult="2,4", model=Value2 | Value4, underlying=6, converted=Value2, Value4
            //
            // Invalid Scenarios:
            // 1. valueProviderResult="2,10", model=12, underlying=12, converted=12
            //
            var underlying = Convert.ChangeType(
                model,
                Enum.GetUnderlyingType(modelType),
                CultureInfo.InvariantCulture).ToString();
            var converted = model.ToString();
            return !string.Equals(underlying, converted, StringComparison.OrdinalIgnoreCase);
        }
        return Enum.IsDefined(modelType, model);
    }
}
