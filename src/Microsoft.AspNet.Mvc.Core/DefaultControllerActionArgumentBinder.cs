// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides a default implementation of <see cref="IControllerActionArgumentBinder"/>.
    /// Uses ModelBinding to populate action parameters.
    /// </summary>
    public class DefaultControllerActionArgumentBinder : IControllerActionArgumentBinder
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IObjectModelValidator _validator;

        public DefaultControllerActionArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IObjectModelValidator validator)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _validator = validator;
        }

        public async Task<IDictionary<string, object>> BindActionArgumentsAsync(
            ActionContext actionContext,
            ActionBindingContext actionBindingContext,
            object controller)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                        nameof(actionContext));
            }

            var operationBindingContext = GetOperationBindingContext(actionContext, actionBindingContext);
            var controllerProperties = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateArgumentsAsync(
                operationBindingContext,
                actionContext.ModelState,
                controllerProperties,
                actionDescriptor.BoundProperties);
            var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
            ActivateProperties(controller, controllerType, controllerProperties);

            var actionArguments = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateArgumentsAsync(
                operationBindingContext,
                actionContext.ModelState,
                actionArguments,
                actionDescriptor.Parameters);
            return actionArguments;
        }

        public async Task<ModelBindingResult> BindModelAsync(
            ParameterDescriptor parameter,
            ModelStateDictionary modelState,
            OperationBindingContext operationContext)
        {
            var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            var parameterType = parameter.ParameterType;
            var modelBindingContext = GetModelBindingContext(
                parameter.Name,
                metadata,
                parameter.BindingInfo,
                modelState,
                operationContext);

            var modelBindingResult = await operationContext.ModelBinder.BindModelAsync(modelBindingContext);
            if (modelBindingResult != null && modelBindingResult.IsModelSet)
            {
                var key = modelBindingResult.Key;
                var modelExplorer = new ModelExplorer(
                    _modelMetadataProvider,
                    metadata,
                    modelBindingResult.Model);

                var validationContext = new ModelValidationContext(
                    key,
                    modelBindingContext.BindingSource,
                    operationContext.ValidatorProvider,
                    modelState,
                    modelExplorer);
                _validator.Validate(validationContext);
            }

            return modelBindingResult;
        }

        private void ActivateProperties(object controller, Type containerType, Dictionary<string, object> properties)
        {
            var propertyHelpers = PropertyHelper.GetProperties(controller);
            foreach (var property in properties)
            {
                var propertyHelper = propertyHelpers.First(helper =>
                    string.Equals(helper.Name, property.Key, StringComparison.Ordinal));
                if (propertyHelper.Property == null || !propertyHelper.Property.CanWrite)
                {
                    // nothing to do
                    return;
                }

                propertyHelper.SetValue(controller, property.Value);
            }
        }

        private async Task PopulateArgumentsAsync(
            OperationBindingContext operationContext,
            ModelStateDictionary modelState,
            IDictionary<string, object> arguments,
            IEnumerable<ParameterDescriptor> parameterMetadata)
        {
            foreach (var parameter in parameterMetadata)
            {
                var modelBindingResult = await BindModelAsync(parameter, modelState, operationContext);
                if (modelBindingResult != null && modelBindingResult.IsModelSet)
                {
                    arguments[parameter.Name] = modelBindingResult.Model;
                }
            }
        }

        private static ModelBindingContext GetModelBindingContext(
            string parameterName,
            ModelMetadata metadata,
            BindingInfo bindingInfo,
            ModelStateDictionary modelState,
            OperationBindingContext operationBindingContext)
        {
            var modelBindingContext = ModelBindingContext.GetModelBindingContext(
                metadata,
                bindingInfo,
                parameterName);

            modelBindingContext.ModelState = modelState;
            modelBindingContext.ValueProvider = operationBindingContext.ValueProvider;
            modelBindingContext.OperationBindingContext = operationBindingContext;

            return modelBindingContext;
        }

        private OperationBindingContext GetOperationBindingContext(
            ActionContext actionContext,
            ActionBindingContext bindingContext)
        {
            return new OperationBindingContext
            {
                ModelBinder = bindingContext.ModelBinder,
                ValidatorProvider = bindingContext.ValidatorProvider,
                MetadataProvider = _modelMetadataProvider,
                HttpContext = actionContext.HttpContext,
                ValueProvider = bindingContext.ValueProvider,
            };
        }
    }
}
