// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which can bind a model based on the value of 
    /// <see cref="ModelMetadata.BinderType"/>. The supplied <see cref="IModelBinder"/> 
    /// type or <see cref="IModelBinderProvider"/> type will be used to bind the model.
    /// </summary>
    public class BinderTypeBasedModelBinder : IModelBinder
    {
        private readonly Func<Type, ObjectFactory> _createFactory =
            (t) => ActivatorUtilities.CreateFactory(t, Type.EmptyTypes);
        private ConcurrentDictionary<Type, ObjectFactory> _typeActivatorCache =
               new ConcurrentDictionary<Type, ObjectFactory>();


        public async Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.BinderType == null)
            {
                // Return null so that we are able to continue with the default set of model binders,
                // if there is no specific model binder provided.
                return null;
            }

            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;
            var createFactory = _typeActivatorCache.GetOrAdd(bindingContext.BinderType, _createFactory);
            var instance = createFactory(requestServices, arguments: null);

            var modelBinder = instance as IModelBinder;
            if (modelBinder == null)
            {
                var modelBinderProvider = instance as IModelBinderProvider;
                if (modelBinderProvider != null)
                {
                    modelBinder = new CompositeModelBinder(modelBinderProvider.ModelBinders);
                }
                else
                {
                    throw new InvalidOperationException(
                        Resources.FormatBinderType_MustBeIModelBinderOrIModelBinderProvider(
                            bindingContext.BinderType.FullName,
                            typeof(IModelBinder).FullName,
                            typeof(IModelBinderProvider).FullName));
                }
            }

            var result = await modelBinder.BindModelAsync(bindingContext);

            var modelBindingResult = result != null ?
                new ModelBindingResult(result.Model, result.Key, result.IsModelSet) :
                new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: false);

            // A model binder was specified by metadata and this binder handles all such cases.
            // Always tell the model binding system to skip other model binders i.e. return non-null.
            return modelBindingResult;
        }
    }
}
