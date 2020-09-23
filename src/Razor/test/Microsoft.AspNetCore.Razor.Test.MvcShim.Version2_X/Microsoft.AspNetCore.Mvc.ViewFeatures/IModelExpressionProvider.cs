// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public interface IModelExpressionProvider
    {
        ModelExpression CreateModelExpression<TModel, TValue>(
               ViewDataDictionary<TModel> viewData,
               Expression<Func<TModel, TValue>> expression);
    }
}
