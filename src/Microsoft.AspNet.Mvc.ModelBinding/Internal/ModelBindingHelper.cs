// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;

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

            if (!allowNullModel && bindingContext.Model == null)
            {
                var message = Resources.FormatModelBinderUtil_ModelCannotBeNull(requiredType);
                throw new ArgumentException(message, "bindingContext");
            }

            if (bindingContext.Model != null &&
                !bindingContext.ModelType.GetTypeInfo().IsAssignableFrom(requiredType.GetTypeInfo()))
            {
                var message = Resources.FormatModelBinderUtil_ModelInstanceIsWrong(
                    bindingContext.Model.GetType(),
                    requiredType);
                throw new ArgumentException(message, "bindingContext");
            }
        }

        internal static void AddModelErrorBasedOnExceptionType(ModelBindingContext bindingContext, Exception ex)
        {
            if (IsFormatException(ex))
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex);
            }
        }

        internal static bool IsFormatException(Exception ex)
        {
            for (; ex != null; ex = ex.InnerException)
            {
                if (ex is FormatException)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
