// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal static class PageBinderFactory
{
    internal static readonly Func<PageContext, object, Task> NullPropertyBinder = (context, arguments) => Task.CompletedTask;
    internal static readonly PageHandlerBinderDelegate NullHandlerBinder = (context, arguments) => Task.CompletedTask;

    public static Func<PageContext, object, Task> CreatePropertyBinder(
        ParameterBinder parameterBinder,
        IModelMetadataProvider modelMetadataProvider,
        IModelBinderFactory modelBinderFactory,
        CompiledPageActionDescriptor actionDescriptor)
    {
        ArgumentNullException.ThrowIfNull(parameterBinder);
        ArgumentNullException.ThrowIfNull(actionDescriptor);

        var properties = actionDescriptor.BoundProperties;
        if (properties == null || properties.Count == 0)
        {
            return NullPropertyBinder;
        }

        var handlerType = actionDescriptor.HandlerTypeInfo.AsType();
        var propertyBindingInfo = new BinderItem[properties.Count];
        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var metadata = modelMetadataProvider.GetMetadataForProperty(handlerType, property.Name);
            var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = property.BindingInfo,
                Metadata = metadata,
                CacheToken = property,
            });

            propertyBindingInfo[i] = new BinderItem(binder, metadata);
        }

        return Bind;

        async Task Bind(PageContext pageContext, object instance)
        {
            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(pageContext, pageContext.ValueProviderFactories);
            if (!success)
            {
                return;
            }

            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                var bindingInfo = propertyBindingInfo[i];
                var modelMetadata = bindingInfo.ModelMetadata;

                if (!modelMetadata.IsBindingAllowed)
                {
                    continue;
                }

                var result = await parameterBinder.BindModelAsync(
                   pageContext,
                   bindingInfo.ModelBinder,
                   valueProvider!,
                   property,
                   modelMetadata,
                   value: null,
                   container: instance);

                if (result.IsModelSet)
                {
                    PropertyValueSetter.SetValue(bindingInfo.ModelMetadata, instance, result.Model);
                }
            }
        }
    }

    public static PageHandlerBinderDelegate CreateHandlerBinder(
        ParameterBinder parameterBinder,
        IModelMetadataProvider modelMetadataProvider,
        IModelBinderFactory modelBinderFactory,
        CompiledPageActionDescriptor actionDescriptor,
        HandlerMethodDescriptor handler)
    {
        if (handler.Parameters == null || handler.Parameters.Count == 0)
        {
            return NullHandlerBinder;
        }

        var handlerType = actionDescriptor.HandlerTypeInfo.AsType();
        var parameterBindingInfo = new BinderItem[handler.Parameters.Count];
        for (var i = 0; i < parameterBindingInfo.Length; i++)
        {
            var parameter = handler.Parameters[i];
            ModelMetadata metadata;
            if (modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase)
            {
                // The default model metadata provider derives from ModelMetadataProvider
                // and can therefore supply information about attributes applied to parameters.
                metadata = modelMetadataProviderBase.GetMetadataForParameter(parameter.ParameterInfo);
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

        return Bind;

        async Task Bind(PageContext pageContext, IDictionary<string, object?> arguments)
        {
            var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(pageContext, pageContext.ValueProviderFactories);
            if (!success)
            {
                return;
            }

            for (var i = 0; i < parameterBindingInfo.Length; i++)
            {
                var parameter = handler.Parameters[i];
                var bindingInfo = parameterBindingInfo[i];
                var modelMetadata = bindingInfo.ModelMetadata;

                if (!modelMetadata.IsBindingAllowed)
                {
                    continue;
                }

                var result = await parameterBinder.BindModelAsync(
                    pageContext,
                    bindingInfo.ModelBinder,
                    valueProvider!,
                    parameter,
                    modelMetadata,
                    value: null,
                    container: null); // Parameters do not have containers.

                if (result.IsModelSet)
                {
                    arguments[parameter.Name] = result.Model;
                }
            }
        }
    }

    private readonly struct BinderItem
    {
        public BinderItem(IModelBinder modelBinder, ModelMetadata modelMetadata)
        {
            ModelMetadata = modelMetadata;
            ModelBinder = modelBinder;
        }

        public ModelMetadata ModelMetadata { get; }

        public IModelBinder ModelBinder { get; }
    }
}
