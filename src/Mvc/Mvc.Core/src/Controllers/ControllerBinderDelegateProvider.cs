// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    // Note: changes made to binding behavior in type should also be made to PageBinderFactory.
    internal static class ControllerBinderDelegateProvider
    {
        public static ControllerBinderDelegate CreateBinderDelegate(
            ParameterBinder parameterBinder,
            IModelBinderFactory modelBinderFactory,
            IModelMetadataProvider modelMetadataProvider,
            ControllerActionDescriptor actionDescriptor,
            MvcOptions mvcOptions)
        {
            if (parameterBinder == null)
            {
                throw new ArgumentNullException(nameof(parameterBinder));
            }

            if (modelBinderFactory == null)
            {
                throw new ArgumentNullException(nameof(modelBinderFactory));
            }

            if (modelMetadataProvider == null)
            {
                throw new ArgumentNullException(nameof(modelMetadataProvider));
            }

            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (mvcOptions == null)
            {
                throw new ArgumentNullException(nameof(mvcOptions));
            }

            var parameterBindingInfo = GetParameterBindingInfo(
                modelBinderFactory,
                modelMetadataProvider,
                actionDescriptor,
                mvcOptions);
            var propertyBindingInfo = GetPropertyBindingInfo(modelBinderFactory, modelMetadataProvider, actionDescriptor);

            if (parameterBindingInfo == null && propertyBindingInfo == null)
            {
                return null;
            }

            return Bind;

            async Task Bind(ControllerContext controllerContext, object controller, Dictionary<string, object> arguments)
            {
                var (success, valueProvider) = await CompositeValueProvider.TryCreateAsync(controllerContext, controllerContext.ValueProviderFactories);
                if (!success)
                {
                    return;
                }

                var parameters = actionDescriptor.Parameters;

                for (var i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];
                    var bindingInfo = parameterBindingInfo[i];
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
                        value: null);

                    if (result.IsModelSet)
                    {
                        arguments[parameter.Name] = result.Model;
                    }
                }

                var properties = actionDescriptor.BoundProperties;
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
                       controllerContext,
                       bindingInfo.ModelBinder,
                       valueProvider,
                       property,
                       modelMetadata,
                       value: null);

                    if (result.IsModelSet)
                    {
                        PropertyValueSetter.SetValue(bindingInfo.ModelMetadata, controller, result.Model);
                    }
                }
            }
        }

        private static BinderItem[] GetParameterBindingInfo(
            IModelBinderFactory modelBinderFactory,
            IModelMetadataProvider modelMetadataProvider,
            ControllerActionDescriptor actionDescriptor,
            MvcOptions mvcOptions)
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

        private static BinderItem[] GetPropertyBindingInfo(
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
}
