// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
            if (String.IsNullOrEmpty(prefix))
            {
                return propertyName ?? String.Empty;
            }
            else if (String.IsNullOrEmpty(propertyName))
            {
                return prefix ?? String.Empty;
            }
            else
            {
                return prefix + "." + propertyName;
            }
        }

        internal static Type GetPossibleBinderInstanceType(Type closedModelType, Type openModelType, Type openBinderType)
        {
            Type[] typeArguments = TypeExtensions.GetTypeArgumentsIfMatch(closedModelType, openModelType);
            return (typeArguments != null) ? openBinderType.MakeGenericType(typeArguments) : null;
        }

        internal static void ReplaceEmptyStringWithNull(ModelMetadata modelMetadata, ref object model)
        {
            if (model is string &&
                modelMetadata.ConvertEmptyStringToNull &&
                String.IsNullOrWhiteSpace(model as string))
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

        internal static void ValidateBindingContext(ModelBindingContext bindingContext, Type requiredType, bool allowNullModel)
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

            if (bindingContext.Model != null && !bindingContext.ModelType.GetTypeInfo().IsAssignableFrom(requiredType.GetTypeInfo()))
            {
                var message = Resources.FormatModelBinderUtil_ModelInstanceIsWrong(
                    bindingContext.Model.GetType(), 
                    requiredType);
                throw new ArgumentException(message, "bindingContext");
            }
        }
    }
}
