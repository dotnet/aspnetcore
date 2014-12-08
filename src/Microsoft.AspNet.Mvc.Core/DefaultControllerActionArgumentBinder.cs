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
        private readonly IModelMetadataProvider _modelMetadataProvider;

        public DefaultControllerActionArgumentBinder(IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadataProvider = modelMetadataProvider;
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

            var parameterMetadata = new List<ModelMetadata>();
            foreach (var parameter in actionDescriptor.Parameters)
            {
                var metadata = _modelMetadataProvider.GetMetadataForParameter(
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
            await PopulateArgumentAsync(actionContext, actionBindingContext, actionArguments, parameterMetadata);
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
            ActionContext actionContext,
            ActionBindingContext bindingContext,
            IDictionary<string, object> arguments,
            IEnumerable<ModelMetadata> parameterMetadata)
        {
            var operationBindingContext = new OperationBindingContext
            {
                ModelBinder = bindingContext.ModelBinder,
                ValidatorProvider = bindingContext.ValidatorProvider,
                MetadataProvider = _modelMetadataProvider,
                HttpContext = actionContext.HttpContext,
                ValueProvider = bindingContext.ValueProvider,
            };

            foreach (var parameter in parameterMetadata)
            {
                var parameterType = parameter.ModelType;
                var modelBindingContext = GetModelBindingContext(parameter, actionContext, operationBindingContext);
                if (await bindingContext.ModelBinder.BindModelAsync(modelBindingContext))
                {
                    arguments[parameter.PropertyName] = modelBindingContext.Model;
                }
            }
        }

        internal static ModelBindingContext GetModelBindingContext(
            ModelMetadata modelMetadata,
            ActionContext actionContext,
            OperationBindingContext operationBindingContext)
        {
            var modelBindingContext = new ModelBindingContext
            {
                ModelName = modelMetadata.BinderModelName ?? modelMetadata.PropertyName,
                ModelMetadata = modelMetadata,
                ModelState = actionContext.ModelState,

                // Fallback only if there is no explicit model name set.
                FallbackToEmptyPrefix = modelMetadata.BinderModelName == null,
                ValueProvider = operationBindingContext.ValueProvider,
                OperationBindingContext = operationBindingContext,
            };

            return modelBindingContext;
        }
    }
}
