// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> for <see cref="double"/> and <see cref="Nullable{T}"/> where <c>T</c> is
/// <see cref="double"/>.
/// </summary>
public class DoubleModelBinder : IModelBinder
{
    private readonly NumberStyles _supportedStyles;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DoubleModelBinder"/>.
    /// </summary>
    /// <param name="supportedStyles">The <see cref="NumberStyles"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DoubleModelBinder(NumberStyles supportedStyles, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _supportedStyles = supportedStyles;
        _logger = loggerFactory.CreateLogger(typeof(DoubleModelBinder));
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        _logger.AttemptingToBindModel(bindingContext);

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            _logger.FoundNoValueInRequest(bindingContext);

            // no entry
            _logger.DoneAttemptingToBindModel(bindingContext);
            return Task.CompletedTask;
        }

        var modelState = bindingContext.ModelState;
        modelState.SetModelValue(modelName, valueProviderResult);

        var metadata = bindingContext.ModelMetadata;
        var type = metadata.UnderlyingOrModelType;
        try
        {
            var value = valueProviderResult.FirstValue;

            object? model;
            if (string.IsNullOrWhiteSpace(value))
            {
                // Parse() method trims the value (with common NumberStyles) then throws if the result is empty.
                model = null;
            }
            else if (type == typeof(double))
            {
                model = double.Parse(value, _supportedStyles, valueProviderResult.Culture);
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
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Success(model);
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
        }

        _logger.DoneAttemptingToBindModel(bindingContext);
        return Task.CompletedTask;
    }
}
