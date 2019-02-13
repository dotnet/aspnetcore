// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public static class ParameterBinderExtensions
    {
        public static Task<ModelBindingResult> BindModelAsync(
            this ParameterBinder parameterBinder,
            ParameterDescriptor parameter,
            ControllerContext context)
        {
            var optionsAccessor = context.HttpContext.RequestServices.GetService<IOptions<MvcOptions>>();
            Assert.NotNull(optionsAccessor?.Value); // Guard
            var modelMetadataProvider = context.HttpContext.RequestServices.GetService<IModelMetadataProvider>();
            Assert.NotNull(modelMetadataProvider); // Guard

            // Imitate a bit of ControllerBinderDelegateProvider and PageBinderFactory
            ParameterInfo parameterInfo;
            if (parameter is ControllerParameterDescriptor controllerParameterDescriptor)
            {
                parameterInfo = controllerParameterDescriptor.ParameterInfo;
            }
            else if (parameter is HandlerParameterDescriptor handlerParameterDescriptor)
            {
                parameterInfo = handlerParameterDescriptor.ParameterInfo;
            }
            else
            {
                parameterInfo = null;
            }

            ModelMetadata metadata;
            if (optionsAccessor.Value.AllowValidatingTopLevelNodes &&
                modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase &&
                parameterInfo != null)
            {
                metadata = modelMetadataProviderBase.GetMetadataForParameter(parameterInfo);
            }
            else
            {
                metadata = modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            }

            return parameterBinder.BindModelAsync(parameter, context, modelMetadataProvider, metadata);
        }

        public static async Task<ModelBindingResult> BindModelAsync(
            this ParameterBinder parameterBinder,
            ParameterDescriptor parameter,
            ControllerContext context,
            IModelMetadataProvider modelMetadataProvider,
            ModelMetadata modelMetadata)
        {
            var valueProvider = await CompositeValueProvider.CreateAsync(context);
            var modelBinderFactory = ModelBindingTestHelper.GetModelBinderFactory(
                modelMetadataProvider,
                context.HttpContext.RequestServices);

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
