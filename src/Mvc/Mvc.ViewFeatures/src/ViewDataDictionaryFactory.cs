// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class ViewDataDictionaryFactory
{
    public static Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> CreateFactory(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var type = typeof(ViewDataDictionary<>).MakeGenericType(modelType);
        var constructor = type.GetConstructor(new[] { typeof(IModelMetadataProvider), typeof(ModelStateDictionary) });
        Debug.Assert(constructor != null);

        var parameter1 = Expression.Parameter(typeof(IModelMetadataProvider), "metadataProvider");
        var parameter2 = Expression.Parameter(typeof(ModelStateDictionary), "modelState");

        return
            Expression.Lambda<Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary>>(
                Expression.Convert(
                    Expression.New(constructor, parameter1, parameter2),
                    typeof(ViewDataDictionary)),
                parameter1,
                parameter2)
            .Compile();
    }

    public static Func<ViewDataDictionary, ViewDataDictionary> CreateNestedFactory(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var type = typeof(ViewDataDictionary<>).MakeGenericType(modelType);
        var constructor = type.GetConstructor(new[] { typeof(ViewDataDictionary) });
        Debug.Assert(constructor != null);

        var parameter = Expression.Parameter(typeof(ViewDataDictionary), "viewDataDictionary");

        return
            Expression.Lambda<Func<ViewDataDictionary, ViewDataDictionary>>(
                Expression.Convert(
                    Expression.New(constructor, parameter),
                    typeof(ViewDataDictionary)),
                parameter)
            .Compile();
    }
}
