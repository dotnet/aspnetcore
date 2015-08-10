// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class GenericModelBinder : IModelBinder
    {
        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            var binderType = ResolveBinderType(bindingContext);
            if (binderType != null)
            {
                var binder = (IModelBinder)Activator.CreateInstance(binderType);

                var collectionBinder = binder as ICollectionModelBinder;
                if (collectionBinder != null &&
                    bindingContext.Model == null &&
                    !collectionBinder.CanCreateInstance(bindingContext.ModelType))
                {
                    // Able to resolve a binder type but need a new model instance and that binder cannot create it.
                    return null;
                }

                var result = await binder.BindModelAsync(bindingContext);
                var modelBindingResult = result != null ?
                    result :
                    new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);

                // Were able to resolve a binder type.
                // Always tell the model binding system to skip other model binders i.e. return non-null.
                return modelBindingResult;
            }

            return null;
        }

        private static Type ResolveBinderType(ModelBindingContext context)
        {
            var modelType = context.ModelType;

            return GetArrayBinder(modelType) ??
                GetCollectionBinder(modelType) ??
                GetDictionaryBinder(modelType) ??
                GetEnumerableBinder(context) ??
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
                typeof(CollectionModelBinder<>),
                modelType);
        }

        private static Type GetDictionaryBinder(Type modelType)
        {
            return GetGenericBinderType(
                typeof(IDictionary<,>),
                typeof(DictionaryModelBinder<,>),
                modelType);
        }

        private static Type GetEnumerableBinder(ModelBindingContext context)
        {
            var modelTypeArguments = GetGenericBinderTypeArgs(typeof(IEnumerable<>), context.ModelType);
            if (modelTypeArguments == null)
            {
                return null;
            }

            if (context.Model == null)
            {
                // GetCollectionBinder has already confirmed modelType is not compatible with ICollection<T>. Can a
                // List<T> (the default CollectionModelBinder type) instance be used instead of that exact type?
                // Likely this will succeed only if the property type is exactly IEnumerable<T>.
                var closedListType = typeof(List<>).MakeGenericType(modelTypeArguments);
                if (!context.ModelType.IsAssignableFrom(closedListType))
                {
                    return null;
                }
            }
            else
            {
                // A non-null instance must be updated in-place. For that the instance must also implement
                // ICollection<T>. For example an IEnumerable<T> property may have a List<T> default value.
                var closedCollectionType = typeof(ICollection<>).MakeGenericType(modelTypeArguments);
                if (!closedCollectionType.IsAssignableFrom(context.Model.GetType()))
                {
                    return null;
                }
            }

            return typeof(CollectionModelBinder<>).MakeGenericType(modelTypeArguments);
        }

        private static Type GetKeyValuePairBinder(Type modelType)
        {
            var modelTypeInfo = modelType.GetTypeInfo();
            if (modelTypeInfo.IsGenericType &&
                modelTypeInfo.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return typeof(KeyValuePairModelBinder<,>).MakeGenericType(modelTypeInfo.GenericTypeArguments);
            }

            return null;
        }

        // Example: GetGenericBinderType(typeof(IList<T>), typeof(ListBinder<T>), ...) means that the ListBinder<T>
        // type can update models that implement IList<T>. This method will return
        // ListBinder<T> or null, depending on whether the type checks succeed.
        private static Type GetGenericBinderType(Type supportedInterfaceType, Type openBinderType, Type modelType)
        {
            Debug.Assert(openBinderType != null);

            var modelTypeArguments = GetGenericBinderTypeArgs(supportedInterfaceType, modelType);
            if (modelTypeArguments == null)
            {
                return null;
            }

            return openBinderType.MakeGenericType(modelTypeArguments);
        }

        // Get the generic arguments for the binder, based on the model type. Or null if not compatible.
        private static Type[] GetGenericBinderTypeArgs(Type supportedInterfaceType, Type modelType)
        {
            Debug.Assert(supportedInterfaceType != null);
            Debug.Assert(modelType != null);

            var modelTypeInfo = modelType.GetTypeInfo();
            if (!modelTypeInfo.IsGenericType || modelTypeInfo.IsGenericTypeDefinition)
            {
                // modelType is not a closed generic type.
                return null;
            }

            var modelTypeArguments = modelTypeInfo.GenericTypeArguments;
            if (modelTypeArguments.Length != supportedInterfaceType.GetTypeInfo().GenericTypeParameters.Length)
            {
                // Wrong number of generic type arguments.
                return null;
            }

            var closedInstanceType = supportedInterfaceType.MakeGenericType(modelTypeArguments);
            if (!closedInstanceType.IsAssignableFrom(modelType))
            {
                // modelType is not compatible with supportedInterfaceType.
                return null;
            }

            return modelTypeArguments;
        }
    }
}
