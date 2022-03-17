// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Provides <see cref="ModelExpression"/> for a Lambda expression.
/// </summary>
public interface IModelExpressionProvider
{
    /// <summary>
    /// Returns a <see cref="ModelExpression"/> instance describing the given <paramref name="expression"/>.
    /// </summary>
    /// <typeparam name="TModel">The type of the <paramref name="viewData"/>'s <see cref="ViewDataDictionary{T}.Model"/>.</typeparam>
    /// <typeparam name="TValue">The type of the <paramref name="expression"/> result.</typeparam>
    /// <param name="viewData">The <see cref="ViewDataDictionary{TModel}"/> containing the <see cref="ViewDataDictionary{T}.Model"/>
    /// against which <paramref name="expression"/> is evaluated. </param>
    /// <param name="expression">An expression to be evaluated against the current model.</param>
    /// <returns>A new <see cref="ModelExpression"/> instance describing the given <paramref name="expression"/>.</returns>
    ModelExpression CreateModelExpression<TModel, TValue>(
        ViewDataDictionary<TModel> viewData,
        Expression<Func<TModel, TValue>> expression);
}
