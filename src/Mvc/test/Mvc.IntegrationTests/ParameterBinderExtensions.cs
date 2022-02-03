// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

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
        if (modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase &&
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
