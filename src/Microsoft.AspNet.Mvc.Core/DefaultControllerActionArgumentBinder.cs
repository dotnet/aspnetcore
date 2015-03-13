// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
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
        private readonly MvcOptions _options;
        private readonly IObjectModelValidator _validator;

        public DefaultControllerActionArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IObjectModelValidator validator,
            IOptions<MvcOptions> optionsAccessor)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _options = optionsAccessor.Options;
            _validator = validator;
        }

        public async Task<IDictionary<string, object>> GetActionArgumentsAsync(
            ActionContext actionContext,
            ActionBindingContext actionBindingContext)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                        nameof(actionContext));
            }

            var actionArguments = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateArgumentsAsync(
                actionContext,
                actionBindingContext,
                actionArguments,
                actionDescriptor.Parameters);
            return actionArguments;
        }   

        private async Task PopulateArgumentsAsync(
            ActionContext actionContext,
            ActionBindingContext bindingContext,
            IDictionary<string, object> arguments,
            IEnumerable<ParameterDescriptor> parameterMetadata)
        {
            var operationBindingContext = new OperationBindingContext
            {
                ModelBinder = bindingContext.ModelBinder,
                ValidatorProvider = bindingContext.ValidatorProvider,
                MetadataProvider = _modelMetadataProvider,
                HttpContext = actionContext.HttpContext,
                ValueProvider = bindingContext.ValueProvider,
            };

            var modelState = actionContext.ModelState;
            modelState.MaxAllowedErrors = _options.MaxModelValidationErrors;
            foreach (var parameter in parameterMetadata)
            {
                var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
                var parameterType = parameter.ParameterType;
                var modelBindingContext = GetModelBindingContext(
                    parameter.Name,
                    metadata,
                    parameter.BindingInfo,
                    modelState,
                    operationBindingContext);

                var modelBindingResult = await bindingContext.ModelBinder.BindModelAsync(modelBindingContext);
                if (modelBindingResult != null && modelBindingResult.IsModelSet)
                {
                    var modelExplorer = new ModelExplorer(
                        _modelMetadataProvider,
                        metadata,
                        modelBindingResult.Model);

                    arguments[parameter.Name] = modelBindingResult.Model;
                    var validationContext = new ModelValidationContext(
                        modelBindingResult.Key,
                        bindingContext.ValidatorProvider,
                        actionContext.ModelState,
                        modelExplorer);
                    _validator.Validate(validationContext);
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
    }
}
