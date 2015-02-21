// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Internal
{
    public static class ModelBindingHelper
    {
        internal static TModel CastOrDefault<TModel>(object model)
        {
            return (model is TModel) ? (TModel)model : default(TModel);
        }

        internal static string CreateIndexModelName(string parentName, int index)
        {
            return CreateIndexModelName(parentName, index.ToString(CultureInfo.InvariantCulture));
        }

        internal static string CreateIndexModelName(string parentName, string index)
        {
            return (parentName.Length == 0) ? "[" + index + "]" : parentName + "[" + index + "]";
        }

        internal static string CreatePropertyModelName(string prefix, string propertyName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return propertyName ?? string.Empty;
            }
            else if (string.IsNullOrEmpty(propertyName))
            {
                return prefix ?? string.Empty;
            }
            else
            {
                return prefix + "." + propertyName;
            }
        }

        internal static Type GetPossibleBinderInstanceType(Type closedModelType,
                                                           Type openModelType,
                                                           Type openBinderType)
        {
            var typeArguments = TypeExtensions.GetTypeArgumentsIfMatch(closedModelType, openModelType);
            return (typeArguments != null) ? openBinderType.MakeGenericType(typeArguments) : null;
        }

        internal static void ReplaceEmptyStringWithNull(ModelMetadata modelMetadata, ref object model)
        {
            if (model is string &&
                modelMetadata.ConvertEmptyStringToNull &&
                string.IsNullOrWhiteSpace(model as string))
            {
                model = null;
            }
        }

        internal static void ValidateBindingContext([NotNull] ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata == null)
            {
                throw new ArgumentException(Resources.ModelBinderUtil_ModelMetadataCannotBeNull, "bindingContext");
            }
        }

        internal static void ValidateBindingContext(ModelBindingContext bindingContext,
                                                    Type requiredType,
                                                    bool allowNullModel)
        {
            ValidateBindingContext(bindingContext);

            if (bindingContext.ModelType != requiredType)
            {
                var message = Resources.FormatModelBinderUtil_ModelTypeIsWrong(bindingContext.ModelType, requiredType);
                throw new ArgumentException(message, "bindingContext");
            }

            if (!allowNullModel && bindingContext.ModelMetadata.Model == null)
            {
                var message = Resources.FormatModelBinderUtil_ModelCannotBeNull(requiredType);
                throw new ArgumentException(message, "bindingContext");
            }

            if (bindingContext.ModelMetadata.Model != null &&
                !bindingContext.ModelType.GetTypeInfo().IsAssignableFrom(requiredType.GetTypeInfo()))
            {
                var message = Resources.FormatModelBinderUtil_ModelInstanceIsWrong(
                    bindingContext.ModelMetadata.Model.GetType(),
                    requiredType);
                throw new ArgumentException(message, "bindingContext");
            }
        }

        public static object ConvertValuesToCollectionType<T>(Type modelType, IList<T> values)
        {
            // There's a limited set of collection types we can support here.
            //
            // For the simple cases - choose a T[] or List<T> if the destination type supports
            // it.
            //
            // For more complex cases, if the destination type is a class and implements ICollection<T>
            // then activate it and add the values.
            //
            // Otherwise just give up.
            if (typeof(List<T>).IsAssignableFrom(modelType))
            {
                return new List<T>(values);
            }
            else if (typeof(T[]).IsAssignableFrom(modelType))
            {
                return values.ToArray();
            }
            else if (
                modelType.GetTypeInfo().IsClass &&
                !modelType.GetTypeInfo().IsAbstract &&
                typeof(ICollection<T>).IsAssignableFrom(modelType))
            {
                var result = (ICollection<T>)Activator.CreateInstance(modelType);
                foreach (var value in values)
                {
                    result.Add(value);
                }

                return result;
            }
            else if (typeof(IEnumerable<T>).IsAssignableFrom(modelType))
            {
                return values;
            }
            else
            {
                return null;
            }
        }
    }
}
