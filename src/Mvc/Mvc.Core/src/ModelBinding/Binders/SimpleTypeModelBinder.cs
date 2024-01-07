// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> for simple types.
/// </summary>
public class SimpleTypeModelBinder : IModelBinder
{
    private readonly TypeConverter _typeConverter;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SimpleTypeModelBinder"/>.
    /// </summary>
    /// <param name="type">The type to create binder for.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public SimpleTypeModelBinder(Type type, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _typeConverter = TypeDescriptor.GetConverter(type);
        _logger = loggerFactory.CreateLogger(typeof(SimpleTypeModelBinder));
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        _logger.AttemptingToBindModel(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            _logger.FoundNoValueInRequest(bindingContext);

            // no entry
            _logger.DoneAttemptingToBindModel(bindingContext);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        try
        {
            var value = bindingContext.ModelMetadata.IsFlagsEnum
                ? valueProviderResult.Values.ToString()
                : valueProviderResult.FirstValue;

            object? model;
            if (bindingContext.ModelType == typeof(string))
            {
                // Already have a string. No further conversion required but handle ConvertEmptyStringToNull.
                if (bindingContext.ModelMetadata.ConvertEmptyStringToNull && string.IsNullOrWhiteSpace(value))
                {
                    model = null;
                }
                else
                {
                    model = value;
                }
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                // Other than the StringConverter, converters Trim() the value then throw if the result is empty.
                model = null;
            }
            else
            {
                model = _typeConverter.ConvertFrom(
                    context: null,
                    culture: valueProviderResult.Culture,
                    value: value);
            }

            CheckModel(bindingContext, valueProviderResult, model);
        }
        catch (Exception exception)
        {
            var isFormatException = exception is FormatException;
            if (!isFormatException && exception.InnerException != null)
            {
                // TypeConverter throws System.Exception wrapping the FormatException,
                // so we capture the inner exception.
                exception = ExceptionDispatchInfo.Capture(exception.InnerException).SourceException;
            }

            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                exception,
                bindingContext.ModelMetadata);
        }

        _logger.DoneAttemptingToBindModel(bindingContext);
        return Task.CompletedTask;
    }

    /// <summary>
    /// If the <paramref name="model" /> is <see langword="null" />, verifies that it is allowed to be <see langword="null" />,
    /// otherwise notifies the <see cref="P:ModelBindingContext.ModelState" /> about the invalid <paramref name="valueProviderResult" />.
    /// Sets the <see href="P:ModelBindingContext.Result" /> to the <paramref name="model" /> if successful.
    /// </summary>
    protected virtual void CheckModel(
        ModelBindingContext bindingContext,
        ValueProviderResult valueProviderResult,
        object? model)
    {
        // When converting newModel a null value may indicate a failed conversion for an otherwise required
        // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
        // current bindingContext. If not, an error is logged.
        if (model == null && !bindingContext.ModelMetadata.IsReferenceOrNullableType)
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                    valueProviderResult.ToString()));
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(model);
        }
    }
}
