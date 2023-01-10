// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Extensions methods for <see cref="ModelStateDictionary"/>.
/// </summary>
public static class ModelStateDictionaryExtensions
{
    /// <summary>
    /// Adds the specified <paramref name="errorMessage"/> to the <see cref="ModelStateEntry.Errors"/> instance
    /// that is associated with the specified <paramref name="expression"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    /// <param name="errorMessage">The error message to add.</param>
    public static void AddModelError<TModel>(
        this ModelStateDictionary modelState,
        Expression<Func<TModel, object>> expression,
        string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(errorMessage);

        modelState.AddModelError(GetExpressionText(expression), errorMessage);
    }

    /// <summary>
    /// Adds the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/> instance
    /// that is associated with the specified <paramref name="expression"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <remarks>
    /// This method allows adding the <paramref name="exception"/> to the current <see cref="ModelStateDictionary"/>
    /// when <see cref="ModelMetadata"/> is not available or the exact <paramref name="exception"/>
    /// must be maintained for later use (even if it is for example a <see cref="FormatException"/>).
    /// </remarks>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    /// <param name="exception">The <see cref="Exception"/> to add.</param>
    public static void TryAddModelException<TModel>(
        this ModelStateDictionary modelState,
        Expression<Func<TModel, object>> expression,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(expression);

        modelState.TryAddModelException(GetExpressionText(expression), exception);
    }

    /// <summary>
    /// Adds the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/> instance
    /// that is associated with the specified <paramref name="expression"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    /// <param name="exception">The <see cref="Exception"/> to add.</param>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
    public static void AddModelError<TModel>(
        this ModelStateDictionary modelState,
        Expression<Func<TModel, object>> expression,
        Exception exception,
        ModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(metadata);

        modelState.AddModelError(GetExpressionText(expression), exception, metadata);
    }

    /// <summary>
    /// Removes the specified <paramref name="expression"/> from the <see cref="ModelStateDictionary"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    /// <returns>
    /// true if the element is successfully removed; otherwise, false.
    /// This method also returns false if <paramref name="expression"/> was not found in the model-state dictionary.
    /// </returns>
    public static bool Remove<TModel>(
        this ModelStateDictionary modelState,
        Expression<Func<TModel, object>> expression)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(expression);

        return modelState.Remove(GetExpressionText(expression));
    }

    /// <summary>
    /// Removes all the entries for the specified <paramref name="expression"/> from the
    /// <see cref="ModelStateDictionary"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="modelState">The <see cref="ModelStateDictionary"/> instance this method extends.</param>
    /// <param name="expression">An expression to be evaluated against an item in the current model.</param>
    public static void RemoveAll<TModel>(
        this ModelStateDictionary modelState,
        Expression<Func<TModel, object>> expression)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(expression);

        string modelKey = GetExpressionText(expression);
        if (string.IsNullOrEmpty(modelKey))
        {
            var modelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(TModel));
            for (var i = 0; i < modelMetadata.Properties.Count; i++)
            {
                var property = modelMetadata.Properties[i];
                var childKey = property.BinderModelName ?? property.PropertyName;
                var entries = modelState.FindKeysWithPrefix(childKey).ToArray();
                foreach (var entry in entries)
                {
                    modelState.Remove(entry.Key);
                }
            }
        }
        else
        {
            var entries = modelState.FindKeysWithPrefix(modelKey).ToArray();
            foreach (var entry in entries)
            {
                modelState.Remove(entry.Key);
            }
        }
    }

    private static string GetExpressionText(LambdaExpression expression)
    {
        // We check if expression is wrapped with conversion to object expression
        // and unwrap it if necessary, because Expression<Func<TModel, object>>
        // automatically creates a convert to object expression for expressions
        // returning value types
        var unaryExpression = expression.Body as UnaryExpression;

        if (IsConversionToObject(unaryExpression))
        {
            return ExpressionHelper.GetUncachedExpressionText(Expression.Lambda(
                unaryExpression.Operand,
                expression.Parameters[0]));
        }

        return ExpressionHelper.GetUncachedExpressionText(expression);
    }

    private static bool IsConversionToObject(UnaryExpression expression)
    {
        return expression?.NodeType == ExpressionType.Convert &&
            expression.Operand?.NodeType == ExpressionType.MemberAccess &&
            expression.Type == typeof(object);
    }
}
