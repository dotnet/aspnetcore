// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides a default implementation of <see cref="IControllerActionArgumentBinder"/>.
    /// Uses ModelBinding to populate action parameters.
    /// </summary>
    public class DefaultControllerActionArgumentBinder : IControllerActionArgumentBinder
    {
        private readonly IActionBindingContextProvider _bindingContextProvider;

        public DefaultControllerActionArgumentBinder(IActionBindingContextProvider bindingContextProvider)
        {
            _bindingContextProvider = bindingContextProvider;
        }

        public async Task<IDictionary<string, object>> GetActionArgumentsAsync(ActionContext actionContext)
        {
            var actionBindingContext = await _bindingContextProvider.GetActionBindingContextAsync(actionContext);
            var metadataProvider = actionBindingContext.MetadataProvider;

            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                        nameof(actionContext));
            }

            var parameterMetadata = new List<ModelMetadata>();
            foreach (var parameter in actionDescriptor.Parameters)
            {
                var metadata = metadataProvider.GetMetadataForParameter(
                    modelAccessor: null,
                    methodInfo: actionDescriptor.MethodInfo,
                    parameterName: parameter.Name);

                if (metadata != null)
                {
                    UpdateParameterMetadata(metadata, parameter.BinderMetadata);
                    parameterMetadata.Add(metadata);
                }
            }

            var actionArguments = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateArgumentAsync(actionBindingContext, actionArguments, parameterMetadata);
            return actionArguments;
        }

        private void UpdateParameterMetadata(ModelMetadata metadata, IBinderMetadata binderMetadata)
        {
            if (binderMetadata != null)
            {
                metadata.BinderMetadata = binderMetadata;
            }

            var nameProvider = binderMetadata as IModelNameProvider;
            if (nameProvider != null && nameProvider.Name != null)
            {
                metadata.BinderModelName = nameProvider.Name;
            }
        }

        private async Task PopulateArgumentAsync(
            ActionBindingContext actionBindingContext,
            IDictionary<string, object> arguments,
            IEnumerable<ModelMetadata> parameterMetadata)
        {
            var operationBindingContext = new OperationBindingContext
            {
                ModelBinder = actionBindingContext.ModelBinder,
                ValidatorProvider = actionBindingContext.ValidatorProvider,
                MetadataProvider = actionBindingContext.MetadataProvider,
                HttpContext = actionBindingContext.ActionContext.HttpContext,
                ValueProvider = actionBindingContext.ValueProvider,
            };

            foreach (var parameter in parameterMetadata)
            {
                var parameterType = parameter.ModelType;
                var modelBindingContext =
                    GetModelBindingContext(parameter, actionBindingContext, operationBindingContext);
                if (await actionBindingContext.ModelBinder.BindModelAsync(modelBindingContext))
                {
                    arguments[parameter.PropertyName] = modelBindingContext.Model;
                }
            }
        }

        internal static ModelBindingContext GetModelBindingContext(
            ModelMetadata modelMetadata,
            ActionBindingContext actionBindingContext,
            OperationBindingContext operationBindingContext)
        {
            var modelBindingContext = new ModelBindingContext
            {
                ModelName = modelMetadata.BinderModelName ?? modelMetadata.PropertyName,
                ModelMetadata = modelMetadata,
                ModelState = actionBindingContext.ActionContext.ModelState,

                // Fallback only if there is no explicit model name set.
                FallbackToEmptyPrefix = modelMetadata.BinderModelName == null,
                ValueProvider = actionBindingContext.ValueProvider,
                OperationBindingContext = operationBindingContext,
            };

            return modelBindingContext;
        }
    }
}
