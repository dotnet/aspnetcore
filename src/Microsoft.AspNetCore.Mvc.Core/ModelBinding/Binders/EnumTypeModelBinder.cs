// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// <see cref="IModelBinder"/> implementation to bind models for types deriving from <see cref="Enum"/>.
    /// </summary>
    public class EnumTypeModelBinder : SimpleTypeModelBinder
    {
        private readonly bool _allowBindingUndefinedValueToEnumType;

        public EnumTypeModelBinder(bool allowBindingUndefinedValueToEnumType, Type modelType)
            : base(modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            _allowBindingUndefinedValueToEnumType = allowBindingUndefinedValueToEnumType;
        }

        protected override void CheckModel(
            ModelBindingContext bindingContext,
            ValueProviderResult valueProviderResult,
            object model)
        {
            if (model == null || _allowBindingUndefinedValueToEnumType)
            {
                base.CheckModel(bindingContext, valueProviderResult, model);
            }
            else
            {
                if (IsDefinedInEnum(model, bindingContext))
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
        }

        private bool IsDefinedInEnum(object model, ModelBindingContext bindingContext)
        {
            var modelType = bindingContext.ModelMetadata.UnderlyingOrModelType;

            // Check if the converted value is indeed defined on the enum as EnumTypeConverter 
            // converts value to the backing type (ex: integer) and does not check if the value is defined on the enum.
            if (bindingContext.ModelMetadata.IsFlagsEnum)
            {
                // Enum.IsDefined does not work with combined flag enum values.
                // From EnumDataTypeAttribute.cs in CoreFX.
                // Exmaples:
                // 
                // [Flags]
                // enum FlagsEnum { Value1 = 1, Value2 = 2, Value4 = 4 }
                //
                // Valid Scenarios:
                // 1. valueproviderresult="Value2,Value4", model=Value2 | Value4, underlying=6, converted=Value2, Value4
                // 2. valueproviderresult="2,4", model=Value2 | Value4, underlying=6, converted=Value2, Value4
                //
                // Invalid Scenarios:
                // 1. valueproviderresult="2,10", model=12, underlying=12, converted=12
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
}
