// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private readonly ITypeActivator _typeActivator;
        
        /// <summary>
        /// Creates a new instance of <see cref="BinderTypeBasedModelBinder"/>.
        /// </summary>
        /// <param name="typeActivator">The <see cref="ITypeActivator"/>.</param>
        public BinderTypeBasedModelBinder([NotNull] ITypeActivator typeActivator)
        {
            _typeActivator = typeActivator;
        }

        public async Task<bool> BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata.BinderType == null)
            {
                // Return false so that we are able to continue with the default set of model binders,
                // if there is no specific model binder provided.
                return false;
            }

            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;
            var instance = _typeActivator.CreateInstance(requestServices, bindingContext.ModelMetadata.BinderType);

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
                            bindingContext.ModelMetadata.BinderType.FullName,
                            typeof(IModelBinder).FullName,
                            typeof(IModelBinderProvider).FullName));
                }
            }

            await modelBinder.BindModelAsync(bindingContext);

            // return true here, because this binder will handle all cases where the model binder is
            // specified by metadata.
            return true;
        }
    }
}
