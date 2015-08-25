// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if DNXCORE50
using System.Reflection;
#endif
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

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
                GetDictionaryBinder(modelType) ??
                GetCollectionBinder(modelType) ??
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
                // ICollection<T>. For example an IEnumerable<T> property may have a List<T> default value. Do not use
                // IsAssignableFrom() because that does not handle explicit interface implementations and binders all
                // perform explicit casts.
                var closedGenericInterface =
                    ClosedGenericMatcher.ExtractGenericInterface(context.Model.GetType(), typeof(ICollection<>));
                if (closedGenericInterface == null)
                {
                    return null;
                }
            }

            return typeof(CollectionModelBinder<>).MakeGenericType(modelTypeArguments);
        }

        private static Type GetKeyValuePairBinder(Type modelType)
        {
            Debug.Assert(modelType != null);

            // Since KeyValuePair is a value type, ExtractGenericInterface() succeeds only on an exact match.
            var closedGenericType = ClosedGenericMatcher.ExtractGenericInterface(modelType, typeof(KeyValuePair<,>));
            if (closedGenericType != null)
            {
                return typeof(KeyValuePairModelBinder<,>).MakeGenericType(modelType.GenericTypeArguments);
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

            var closedGenericInterface =
                ClosedGenericMatcher.ExtractGenericInterface(modelType, supportedInterfaceType);

            return closedGenericInterface?.GenericTypeArguments;
        }
    }
}
