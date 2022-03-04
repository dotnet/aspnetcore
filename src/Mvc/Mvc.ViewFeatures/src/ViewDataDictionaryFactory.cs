// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal static class ViewDataDictionaryFactory
    {
        public static Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> CreateFactory(TypeInfo modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

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

        public static Func<ViewDataDictionary, ViewDataDictionary> CreateNestedFactory(TypeInfo modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

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
}
