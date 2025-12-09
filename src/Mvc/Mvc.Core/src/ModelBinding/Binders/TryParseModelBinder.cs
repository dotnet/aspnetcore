// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> for simple types.
/// </summary>
internal sealed class TryParseModelBinder : IModelBinder
{
    private static readonly MethodInfo AddModelErrorMethod = typeof(TryParseModelBinder).GetMethod(nameof(AddModelError), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo SuccessBindingResultMethod = typeof(ModelBindingResult).GetMethod(nameof(ModelBindingResult.Success), BindingFlags.Public | BindingFlags.Static)!;
    private static readonly ParameterExpression BindingContextExpression = Expression.Parameter(typeof(ModelBindingContext), "bindingContext");
    private static readonly ParameterExpression ValueProviderResultExpression = Expression.Parameter(typeof(ValueProviderResult), "valueProviderResult");
    private static readonly MemberExpression BindingResultExpression = Expression.Property(BindingContextExpression, nameof(ModelBindingContext.Result));
    private static readonly MemberExpression ValueExpression = Expression.Property(ValueProviderResultExpression, nameof(ValueProviderResult.FirstValue));
    private static readonly MemberExpression CultureExpression = Expression.Property(ValueProviderResultExpression, nameof(ValueProviderResult.Culture));

    private readonly Func<ValueProviderResult, ModelBindingContext, object?> _tryParseOperation;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="SimpleTypeModelBinder"/>.
    /// </summary>
    /// <param name="modelType">The model type.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public TryParseModelBinder(Type modelType, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(modelType);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _tryParseOperation = CreateTryParseOperation(modelType);
        _logger = loggerFactory.CreateLogger(typeof(TryParseModelBinder));
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
            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                // Most TryParse() methods trim the value and fail if the result is empty.

                // When converting a null value may indicate a failed conversion for an otherwise required
                // model (can't set a ValueType to null). This detects if a null model value is acceptable given the
                // current bindingContext. If not, an error is logged.
                if (!bindingContext.ModelMetadata.IsReferenceOrNullableType)
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueMustNotBeNullAccessor(
                            valueProviderResult.ToString()));
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Success(null);
                }
            }
            else
            {
                _tryParseOperation(valueProviderResult, bindingContext);
            }
        }
        catch (Exception exception)
        {
            // Conversion failed.
            AddModelError(bindingContext, exception);
        }

        _logger.DoneAttemptingToBindModel(bindingContext);
        return Task.CompletedTask;
    }

    private static void AddModelError(ModelBindingContext bindingContext, Exception exception)
    {
        // Conversion failed.
        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            exception,
            bindingContext.ModelMetadata);
    }

    private static Func<ValueProviderResult, ModelBindingContext, object?> CreateTryParseOperation(Type modelType)
    {
        modelType = Nullable.GetUnderlyingType(modelType) ?? modelType;
        var tryParseMethodExpession = ModelMetadata.FindTryParseMethod(modelType)
            ?? throw new InvalidOperationException(Resources.FormatTryParseModelBinder_InvalidType(modelType, nameof(TryParseModelBinder)));

        // var tempSourceString = valueProviderResult.FirstValue;
        // object model = null;
        // if ([modeltype].TryParse(tempSourceString, [valueProviderResult.Culture,] out [modelType] parsedValue))
        // {
        //     model = (object)parsedValue;
        //     bindingContext.Result = ModelBindingResult.Success(model);
        // }
        // else
        // {
        //     AddModelError(bindingContext, new FormatException());
        // }
        // return model;

        var parsedValue = Expression.Variable(modelType, "parsedValue");
        var modelValue = Expression.Variable(typeof(object), "model");

        var expression = Expression.Block(
            new[] { parsedValue, modelValue, ParameterBindingMethodCache.SharedExpressions.TempSourceStringExpr },
            Expression.Assign(ParameterBindingMethodCache.SharedExpressions.TempSourceStringExpr, ValueExpression),
            Expression.IfThenElse(tryParseMethodExpession(parsedValue, CultureExpression),
                Expression.Block(
                    Expression.Assign(modelValue, Expression.Convert(parsedValue, modelValue.Type)),
                    Expression.Assign(BindingResultExpression, Expression.Call(SuccessBindingResultMethod, modelValue))),
                Expression.Call(AddModelErrorMethod, BindingContextExpression, Expression.Constant(new FormatException()))),
            modelValue);

        return Expression.Lambda<Func<ValueProviderResult, ModelBindingContext, object?>>(expression, new[] { ValueProviderResultExpression, BindingContextExpression }).Compile();
    }
}
