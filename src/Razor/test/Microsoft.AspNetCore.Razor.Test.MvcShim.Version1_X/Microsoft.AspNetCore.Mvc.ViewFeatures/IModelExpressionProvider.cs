// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public interface IModelExpressionProvider
{
    ModelExpression CreateModelExpression<TModel, TValue>(
           ViewDataDictionary<TModel> viewData,
           Expression<Func<TModel, TValue>> expression);
}
