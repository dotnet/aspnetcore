// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinder"/> for <see cref="decimal"/> and <see cref="Nullable{T}"/> where <c>T</c> is
    /// <see cref="decimal"/>.
    /// </summary>
    public class DoubleModelBinder : IModelBinder
    {
        private readonly NumberStyles _supportedStyles;

        public DoubleModelBinder(NumberStyles supportedStyles)
        {
            _supportedStyles = supportedStyles;
        }

        /// <inheritdoc />
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                // no entry
                return Task.CompletedTask;
            }

            var modelState = bindingContext.ModelState;
            modelState.SetModelValue(modelName, valueProviderResult);

            var metadata = bindingContext.ModelMetadata;
            var type = metadata.UnderlyingOrModelType;
            try
            {
                var value = valueProviderResult.FirstValue;
                var culture = valueProviderResult.Culture;

                object model;
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Parse() method trims the value (with common NumberStyles) then throws if the result is empty.
                    model = null;
                }
                else if (type == typeof(double))
                {
                    model = double.Parse(value, _supportedStyles, culture);
                }
                else
                {
                    // unreachable
                    throw new NotSupportedException();
                }

                // When converting value, a null model may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (model == null && !metadata.IsReferenceOrNullableType)
                {
                    modelState.TryAddModelError(
                        modelName,
                        metadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                            valueProviderResult.ToString()));

                    return Task.CompletedTask;
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(model);
                    return Task.CompletedTask;
                }
            }
            catch (Exception exception)
            {
                var isFormatException = exception is FormatException;
                if (!isFormatException && exception.InnerException != null)
                {
                    // Unlike TypeConverters, floating point types do not seem to wrap FormatExceptions. Preserve
                    // this code in case a cursory review of the CoreFx code missed something.
                    exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
                }

                modelState.TryAddModelError(modelName, exception, metadata);

                // Conversion failed.
                return Task.CompletedTask;
            }
        }
    }
}
