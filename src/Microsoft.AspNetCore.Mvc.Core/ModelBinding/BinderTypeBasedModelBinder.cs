// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IModelBinder"/> which can bind a model based on the value of 
    /// <see cref="ModelMetadata.BinderType"/>. The supplied <see cref="IModelBinder"/> 
    /// type will be used to bind the model.
    /// </summary>
    public class BinderTypeBasedModelBinder : IModelBinder
    {
        private readonly Func<Type, ObjectFactory> _createFactory =
            (t) => ActivatorUtilities.CreateFactory(t, Type.EmptyTypes);
        private readonly ConcurrentDictionary<Type, ObjectFactory> _typeActivatorCache =
               new ConcurrentDictionary<Type, ObjectFactory>();

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            // This method is optimized to use cached tasks when possible and avoid allocating
            // using Task.FromResult. If you need to make changes of this nature, profile
            // allocations afterwards and look for Task<ModelBindingResult>.

            if (bindingContext.BinderType == null)
            {
                // Return null so that we are able to continue with the default set of model binders,
                // if there is no specific model binder provided.
                return TaskCache.CompletedTask;
            }

            return BindModelCoreAsync(bindingContext);
        }

        private async Task BindModelCoreAsync(ModelBindingContext bindingContext)
        {
            var requestServices = bindingContext.OperationBindingContext.HttpContext.RequestServices;
            var createFactory = _typeActivatorCache.GetOrAdd(bindingContext.BinderType, _createFactory);
            var instance = createFactory(requestServices, arguments: null);

            var modelBinder = instance as IModelBinder;
            if (modelBinder == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatBinderType_MustBeIModelBinder(
                        bindingContext.BinderType.FullName,
                        typeof(IModelBinder).FullName));
            }

            await modelBinder.BindModelAsync(bindingContext);

            // A model binder was specified by metadata and this binder handles all such cases.
            // Always tell the model binding system to skip other model binders i.e. return non-null.
            if (bindingContext.Result == null)
            {
                bindingContext.Result = ModelBindingResult.Failed(bindingContext.ModelName);
            }
        }
    }
}
