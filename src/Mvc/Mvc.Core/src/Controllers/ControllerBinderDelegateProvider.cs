// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Controllers;

// Note: changes made to binding behavior in type should also be made to PageBinderFactory.
internal static class ControllerBinderDelegateProvider
{
    public static ControllerBinderDelegate? CreateBinderDelegate(
        ParameterBinder parameterBinder,
        IModelBinderFactory modelBinderFactory,
        IModelMetadataProvider modelMetadataProvider,
        ControllerActionDescriptor actionDescriptor,
        MvcOptions mvcOptions)
    {
        ArgumentNullException.ThrowIfNull(parameterBinder);
        ArgumentNullException.ThrowIfNull(modelBinderFactory);
        ArgumentNullException.ThrowIfNull(modelMetadataProvider);
        ArgumentNullException.ThrowIfNull(actionDescriptor);
        ArgumentNullException.ThrowIfNull(mvcOptions);

        var parameterBindingInfo = GetParameterBindingInfo(
            modelBinderFactory,
            modelMetadataProvider,
            actionDescriptor);
        var propertyBindingInfo = GetPropertyBindingInfo(modelBinderFactory, modelMetadataProvider, actionDescriptor);

        if (parameterBindingInfo == null && propertyBindingInfo == null)
        {
            return null;
        }

        var parameters = actionDescriptor.Parameters switch
        {
            List<ParameterDescriptor> list => list.ToArray(),
            _ => actionDescriptor.Parameters.ToArray()
        };

        var properties = actionDescriptor.BoundProperties switch
        {
            List<ParameterDescriptor> list => list.ToArray(),
            _ => actionDescriptor.BoundProperties.ToArray()
        };

        return Bind;

        async Task Bind(ControllerContext controllerContext, object controller, Dictionary<string, object?> arguments)
        {
            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(controllerContext, controllerContext.ValueProviderFactories);
            if (!success)
            {
                return;
            }

            Debug.Assert(valueProvider is not null);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var bindingInfo = parameterBindingInfo![i];
                var modelMetadata = bindingInfo.ModelMetadata;

                if (!modelMetadata.IsBindingAllowed)
                {
                    continue;
                }

                var result = await parameterBinder.BindModelAsync(
                    controllerContext,
                    bindingInfo.ModelBinder,
                    valueProvider,
                    parameter,
                    modelMetadata,
                    value: null,
                    container: null); // Parameters do not have containers.

                if (result.IsModelSet)
                {
                    arguments[parameter.Name] = result.Model;
                }
            }

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var bindingInfo = propertyBindingInfo![i];
                var modelMetadata = bindingInfo.ModelMetadata;

                if (!modelMetadata.IsBindingAllowed)
                {
                    continue;
                }

                var result = await parameterBinder.BindModelAsync(
                   controllerContext,
                   bindingInfo.ModelBinder,
                   valueProvider,
                   property,
                   modelMetadata,
                   value: null,
                   container: controller);

                if (result.IsModelSet)
                {
                    PropertyValueSetter.SetValue(bindingInfo.ModelMetadata, controller, result.Model);
                }
            }
        }
    }

    private static BinderItem[]? GetParameterBindingInfo(
        IModelBinderFactory modelBinderFactory,
        IModelMetadataProvider modelMetadataProvider,
        ControllerActionDescriptor actionDescriptor)
    {
        var parameters = actionDescriptor.Parameters;
        if (parameters.Count == 0)
        {
            return null;
        }

        var parameterBindingInfo = new BinderItem[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            ModelMetadata metadata;
            if (modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase &&
                parameter is ControllerParameterDescriptor controllerParameterDescriptor)
            {
                // The default model metadata provider derives from ModelMetadataProvider
                // and can therefore supply information about attributes applied to parameters.
                metadata = modelMetadataProviderBase.GetMetadataForParameter(controllerParameterDescriptor.ParameterInfo);
            }
            else
            {
                // For backward compatibility, if there's a custom model metadata provider that
                // only implements the older IModelMetadataProvider interface, access the more
                // limited metadata information it supplies. In this scenario, validation attributes
                // are not supported on parameters.
                metadata = modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            }

            var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            parameterBindingInfo[i] = new BinderItem(binder, metadata);
        }

        return parameterBindingInfo;
    }

    private static BinderItem[]? GetPropertyBindingInfo(
        IModelBinderFactory modelBinderFactory,
        IModelMetadataProvider modelMetadataProvider,
        ControllerActionDescriptor actionDescriptor)
    {
        var properties = actionDescriptor.BoundProperties;
        if (properties.Count == 0)
        {
            return null;
        }

        var propertyBindingInfo = new BinderItem[properties.Count];
        var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var metadata = modelMetadataProvider.GetMetadataForProperty(controllerType, property.Name);
            var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = property.BindingInfo,
                Metadata = metadata,
                CacheToken = property,
            });

            propertyBindingInfo[i] = new BinderItem(binder, metadata);
        }

        return propertyBindingInfo;
    }

    private readonly struct BinderItem
    {
        public BinderItem(IModelBinder modelBinder, ModelMetadata modelMetadata)
        {
            ModelBinder = modelBinder;
            ModelMetadata = modelMetadata;
        }

        public IModelBinder ModelBinder { get; }

        public ModelMetadata ModelMetadata { get; }
    }
}
