// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class GenericModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            var binderType = ResolveBinderType(bindingContext.ModelType);
            if (binderType != null)
            {
                var binder = (IModelBinder)Activator.CreateInstance(binderType);
                var result = await binder.BindModelAsync(bindingContext);

                var modelBindingResult = result != null ?
                    new ModelBindingResult(result.Model, result.Key, result.IsModelSet) :
                    new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);

                // Were able to resolve a binder type.
                // Always tell the model binding system to skip other model binders i.e. return non-null.
                return modelBindingResult;
            }

            return null;
        }

        private static Type ResolveBinderType(Type modelType)
        {
            return GetArrayBinder(modelType) ??
                   GetCollectionBinder(modelType) ??
                   GetDictionaryBinder(modelType) ??
                   GetKeyValuePairBinder(modelType);
        }

        private static Type GetArrayBinder(Type modelType)
        {
            if (modelType.IsArray)
            {
                var elementType = modelType.GetElementType();
                return typeof(ArrayModelBinder<>).MakeGenericType(elementType);
            }
            return null;
        }

        private static Type GetCollectionBinder(Type modelType)
        {
            return GetGenericBinderType(
                        typeof(ICollection<>),
                        typeof(List<>),
                        typeof(CollectionModelBinder<>),
                        modelType);
        }

        private static Type GetDictionaryBinder(Type modelType)
        {
            return GetGenericBinderType(
                        typeof(IDictionary<,>),
                        typeof(Dictionary<,>),
                        typeof(DictionaryModelBinder<,>),
                        modelType);
        }

        private static Type GetKeyValuePairBinder(Type modelType)
        {
            return ModelBindingHelper.GetPossibleBinderInstanceType(
                        closedModelType: modelType,
                        openModelType: typeof(KeyValuePair<,>),
                        openBinderType: typeof(KeyValuePairModelBinder<,>));
        }

        /// <remarks>
        /// Example: <c>GetGenericBinderType(typeof(IList&lt;T&gt;), typeof(List&lt;T&gt;),
        /// typeof(ListBinder&lt;T&gt;), ...)</c> means that the <c>ListBinder&lt;T&gt;</c> type can update models that
        /// implement <see cref="IList{T}"/>, and if for some reason the existing model instance is not updatable the
        /// binder will create a <see cref="List{T}"/> object and bind to that instead. This method will return
        /// <c>ListBinder&lt;T&gt;</c> or <c>null</c>, depending on whether the type and updatability checks succeed.
        /// </remarks>
        private static Type GetGenericBinderType(Type supportedInterfaceType,
                                                 Type newInstanceType,
                                                 Type openBinderType,
                                                 Type modelType)
        {
            Debug.Assert(supportedInterfaceType != null);
            Debug.Assert(openBinderType != null);
            Debug.Assert(modelType != null);

            var modelTypeArguments = GetGenericBinderTypeArgs(supportedInterfaceType, modelType);

            if (modelTypeArguments == null)
            {
                return null;
            }

            var closedNewInstanceType = newInstanceType.MakeGenericType(modelTypeArguments);
            if (!modelType.GetTypeInfo().IsAssignableFrom(closedNewInstanceType.GetTypeInfo()))
            {
                return null;
            }

            return openBinderType.MakeGenericType(modelTypeArguments);
        }

        // Get the generic arguments for the binder, based on the model type. Or null if not compatible.
        private static Type[] GetGenericBinderTypeArgs(Type supportedInterfaceType, Type modelType)
        {
            var modelTypeInfo = modelType.GetTypeInfo();
            if (!modelTypeInfo.IsGenericType || modelTypeInfo.IsGenericTypeDefinition)
            {
                // not a closed generic type
                return null;
            }

            var modelTypeArguments = modelTypeInfo.GenericTypeArguments;
            if (modelTypeArguments.Length != supportedInterfaceType.GetTypeInfo().GenericTypeParameters.Length)
            {
                // wrong number of generic type arguments
                return null;
            }

            return modelTypeArguments;
        }
    }
}
