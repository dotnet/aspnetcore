// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
            var parameters = actionContext.ActionDescriptor.Parameters;
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                        nameof(actionContext));
            }

            var actionMethodInfo = actionDescriptor.MethodInfo;
            var parameterMetadatas = metadataProvider.GetMetadataForParameters(actionMethodInfo);

            var actionArguments = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateActionArgumentsAsync(parameterMetadatas, actionBindingContext, actionArguments);
            return actionArguments;
        }

        private async Task PopulateActionArgumentsAsync(IEnumerable<ModelMetadata> modelMetadatas,
                                                        ActionBindingContext actionBindingContext, 
                                                        IDictionary<string, object> invocationInfo)
        {
            var bodyBoundParameterCount = modelMetadatas.Count(
                                            modelMetadata => modelMetadata.Marker is IBodyBinderMarker);
            if (bodyBoundParameterCount > 1)
            {
                throw new InvalidOperationException(Resources.MultipleBodyParametersAreNotAllowed);
            }

            foreach (var modelMetadata in modelMetadatas)
            {
                var modelBindingContext = GetModelBindingContext(modelMetadata, actionBindingContext);

                if (await actionBindingContext.ModelBinder.BindModelAsync(modelBindingContext))
                {
                    invocationInfo[modelMetadata.PropertyName] = modelBindingContext.Model;
                }
            }
        }

        internal static ModelBindingContext GetModelBindingContext(ModelMetadata modelMetadata, ActionBindingContext actionBindingContext)
        {
            var modelBindingContext = new ModelBindingContext
            {
                ModelName = modelMetadata.ModelName ?? modelMetadata.PropertyName,
                ModelMetadata = modelMetadata,
                ModelState = actionBindingContext.ActionContext.ModelState,
                ModelBinder = actionBindingContext.ModelBinder,
                ValidatorProvider = actionBindingContext.ValidatorProvider,
                MetadataProvider = actionBindingContext.MetadataProvider,
                HttpContext = actionBindingContext.ActionContext.HttpContext,

                // Fallback only if there is no explicit model name set.
                FallbackToEmptyPrefix = modelMetadata.ModelName == null,
                ValueProvider = actionBindingContext.ValueProvider,
            };

            return modelBindingContext;
        }
    }
}
