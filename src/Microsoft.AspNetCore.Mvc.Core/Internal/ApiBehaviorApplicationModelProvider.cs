// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiBehaviorApplicationModelProvider : IApplicationModelProvider
    {
        private readonly ApiBehaviorOptions _apiBehaviorOptions;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ModelStateInvalidFilter _modelStateInvalidFilter;
        private readonly ILogger _logger;

        public ApiBehaviorApplicationModelProvider(
            IOptions<ApiBehaviorOptions> apiBehaviorOptions,
            IModelMetadataProvider modelMetadataProvider,
            ILoggerFactory loggerFactory)
        {
            _apiBehaviorOptions = apiBehaviorOptions.Value;
            _modelMetadataProvider = modelMetadataProvider;
            _logger = loggerFactory.CreateLogger<ApiBehaviorApplicationModelProvider>();

            if (!_apiBehaviorOptions.SuppressModelStateInvalidFilter && _apiBehaviorOptions.InvalidModelStateResponseFactory == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    typeof(ApiBehaviorOptions),
                    nameof(ApiBehaviorOptions.InvalidModelStateResponseFactory)));
            }

            _modelStateInvalidFilter = new ModelStateInvalidFilter(
                apiBehaviorOptions.Value,
                loggerFactory.CreateLogger<ModelStateInvalidFilter>());
        }

        /// <remarks>
        /// Order is set to execute after the <see cref="DefaultApplicationModelProvider"/> and allow any other user
        /// <see cref="IApplicationModelProvider"/> that configure routing to execute.
        /// </remarks>
        public int Order => -1000 + 100;

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controllerModel in context.Result.Controllers)
            {
                var isApiController = controllerModel.Attributes.OfType<IApiBehaviorMetadata>().Any();
                if (isApiController &&
                    controllerModel.ApiExplorer.IsVisible == null)
                {
                    // Enable ApiExplorer for the controller if it wasn't already explicitly configured.
                    controllerModel.ApiExplorer.IsVisible = true;
                }

                var controllerHasSelectorModel = controllerModel.Selectors.Any(s => s.AttributeRouteModel != null);

                foreach (var actionModel in controllerModel.Actions)
                {
                    if (!isApiController && !actionModel.Attributes.OfType<IApiBehaviorMetadata>().Any())
                    {
                        continue;
                    }

                    EnsureActionIsAttributeRouted(controllerHasSelectorModel, actionModel);

                    AddInvalidModelStateFilter(actionModel);

                    InferParameterBindingSources(actionModel);

                    AddMultipartFormDataConsumesAttribute(actionModel);
                }
            }
        }

        // Internal for unit testing
        internal void AddMultipartFormDataConsumesAttribute(ActionModel actionModel)
        {
            if (_apiBehaviorOptions.SuppressConsumesConstraintForFormFileParameters)
            {
                return;
            }

            // Add a ConsumesAttribute if the request does not explicitly specify one.
            if (actionModel.Filters.OfType<IConsumesActionConstraint>().Any())
            {
                return;
            }

            foreach (var parameter in actionModel.Parameters)
            {
                var bindingSource = parameter.BindingInfo?.BindingSource;
                if (bindingSource == BindingSource.FormFile)
                {
                    // If an action accepts files, it must accept multipart/form-data.
                    actionModel.Filters.Add(new ConsumesAttribute("multipart/form-data"));
                }
            }
        }

        private static void EnsureActionIsAttributeRouted(bool controllerHasSelectorModel, ActionModel actionModel)
        {
            if (!controllerHasSelectorModel && !actionModel.Selectors.Any(s => s.AttributeRouteModel != null))
            {
                // Require attribute routing with controllers annotated with ApiControllerAttribute
                throw new InvalidOperationException(Resources.FormatApiController_AttributeRouteRequired(nameof(ApiControllerAttribute)));
            }
        }

        private void AddInvalidModelStateFilter(ActionModel actionModel)
        {
            if (_apiBehaviorOptions.SuppressModelStateInvalidFilter)
            {
                return;
            }

            Debug.Assert(_apiBehaviorOptions.InvalidModelStateResponseFactory != null);
            actionModel.Filters.Add(_modelStateInvalidFilter);
        }

        private void InferParameterBindingSources(ActionModel actionModel)
        {
            if (_modelMetadataProvider == null || _apiBehaviorOptions.SuppressInferBindingSourcesForParameters)
            {
                return;
            }
            var inferredBindingSources = new BindingSource[actionModel.Parameters.Count];
            var foundFromBodyParameter = false;

            for (var i = 0; i < inferredBindingSources.Length; i++)
            {
                var parameter = actionModel.Parameters[i];
                var bindingSource = parameter.BindingInfo?.BindingSource;
                if (bindingSource == null)
                {
                    bindingSource = InferBindingSourceForParameter(parameter);
                }

                if (bindingSource == BindingSource.Body)
                {
                    if (foundFromBodyParameter)
                    {
                        // More than one parameter is inferred as FromBody. Log a warning and skip this action.
                        _logger.UnableToInferBindingSource(actionModel);
                    }
                    else
                    {
                        foundFromBodyParameter = true;
                    }
                }

                inferredBindingSources[i] = bindingSource;
            }

            for (var i = 0; i < inferredBindingSources.Length; i++)
            {
                var bindingSource = inferredBindingSources[i];
                if (bindingSource != null)
                {
                    actionModel.Parameters[i].BindingInfo = new BindingInfo
                    {
                        BindingSource = bindingSource,
                    };
                }
            }
        }

        // Internal for unit testing.
        internal BindingSource InferBindingSourceForParameter(ParameterModel parameter)
        {
            var parameterType = parameter.ParameterInfo.ParameterType;
            if (ParameterExistsInAllRoutes(parameter.Action, parameter.ParameterName))
            {
                return BindingSource.Path;
            }
            else
            {
                ModelMetadata parameterMetadata;
                if (_modelMetadataProvider is ModelMetadataProvider modelMetadataProvider)
                {
                    parameterMetadata = modelMetadataProvider.GetMetadataForParameter(parameter.ParameterInfo);
                }
                else
                {
                    parameterMetadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterInfo.ParameterType);
                }

                if (parameterMetadata != null)
                {
                    var bindingSource = parameterMetadata.IsComplexType ?
                        BindingSource.Body :
                        BindingSource.Query;

                    return bindingSource;
                }
            }

            return null;
        }

        private bool ParameterExistsInAllRoutes(ActionModel actionModel, string parameterName)
        {
            var parameterExistsInSomeRoute = false;
            foreach (var (route, _, _) in ActionAttributeRouteModel.GetAttributeRoutes(actionModel))
            {
                if (route == null)
                {
                    continue;
                }

                var parsedTemplate = TemplateParser.Parse(route.Template);
                if (parsedTemplate.GetParameter(parameterName) == null)
                {
                    return false;
                }

                // Ensure at least one route exists.
                parameterExistsInSomeRoute = true;
            }

            return parameterExistsInSomeRoute;
        }
    }
}
