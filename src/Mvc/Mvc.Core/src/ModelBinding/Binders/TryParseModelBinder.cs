// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> for simple types.
/// </summary>
public class TryParseModelBinder : IModelBinder
{
    private readonly Func<ParameterExpression, IFormatProvider, Expression> _tryParseMethodExpession;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SimpleTypeModelBinder"/>.
    /// </summary>
    /// <param name="type">The type to create binder for.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public TryParseModelBinder(Type type, ILoggerFactory loggerFactory)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _tryParseMethodExpession = ModelMetadata.FindTryParseMethod(type)!;
        _logger = loggerFactory.CreateLogger<SimpleTypeModelBinder>();
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

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
            var value = valueProviderResult.FirstValue;

            object? model = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                // Other than the StringConverter, converters Trim() the value then throw if the result is empty.
                model = null;
            }
            else
            {
                var parsedValue = Expression.Variable(bindingContext.ModelType, "parsedValue");
                var modelValue = Expression.Variable(typeof(object), "model");

                var expression = Expression.Block(
                    new[] { parsedValue, modelValue, ParameterBindingMethodCache.TempSourceStringExpr },
                    Expression.Assign(ParameterBindingMethodCache.TempSourceStringExpr, Expression.Constant(value!)),
                    Expression.IfThenElse(_tryParseMethodExpession(parsedValue, valueProviderResult.Culture),
                        Expression.Assign(modelValue, Expression.Convert(parsedValue, modelValue.Type)),
                        Expression.Throw(Expression.Constant(new FormatException()))),
                    modelValue);

                model = Expression.Lambda<Func<object?>>(expression).Compile()();
            }

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
        catch (Exception exception)
        {
            // Conversion failed.
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                exception,
                bindingContext.ModelMetadata);
        }

        _logger.DoneAttemptingToBindModel(bindingContext);
        return Task.CompletedTask;
    }
}
