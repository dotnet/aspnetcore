// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public static class ParameterBinderExtensions
    {
        public static async Task<ModelBindingResult> BindModelAsync(
            this ParameterBinder parameterBinder,
            ParameterDescriptor parameter, 
            ControllerContext context)
        {
            var valueProvider = await CompositeValueProvider.CreateAsync(context);
            
            return await parameterBinder.BindModelAsync(context, valueProvider, parameter);
        }
        public static async Task<ModelBindingResult> BindModelAsync(
            this ParameterBinder parameterBinder,
            ParameterDescriptor parameter,
            ControllerContext context,
            IModelMetadataProvider modelMetadataProvider,
            ModelMetadata modelMetadata)
        {
            var valueProvider = await CompositeValueProvider.CreateAsync(context);

            var modelBinderFactory = ModelBindingTestHelper.GetModelBinderFactory(modelMetadataProvider);

            var modelBinder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = modelMetadata,
                CacheToken = parameter,
            });

            return await parameterBinder.BindModelAsync(
                context,
                modelBinder,
                valueProvider,
                parameter,
                modelMetadata,
                value: null);
        }
    }
}
