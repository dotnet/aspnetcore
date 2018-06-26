// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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

                if (isApiController)
                {
                    InferBoundPropertyModelPrefixes(controllerModel);
                }

                var controllerHasSelectorModel = controllerModel.Selectors.Any(s => s.AttributeRouteModel != null);
                var conventions = controllerModel.Attributes.OfType<ApiConventionTypeAttribute>().ToArray();
                if (conventions.Length == 0)
                {
                    var controllerAssembly = controllerModel.ControllerType.Assembly;
                    conventions = controllerAssembly.GetCustomAttributes<ApiConventionTypeAttribute>().ToArray();
                }

                foreach (var actionModel in controllerModel.Actions)
                {
                    if (!isApiController && !actionModel.Attributes.OfType<IApiBehaviorMetadata>().Any())
                    {
                        continue;
                    }

                    EnsureActionIsAttributeRouted(controllerHasSelectorModel, actionModel);

                    AddInvalidModelStateFilter(actionModel);

                    InferParameterBindingSources(actionModel);

                    InferParameterModelPrefixes(actionModel);

                    AddMultipartFormDataConsumesAttribute(actionModel);

                    DiscoverApiConvention(actionModel, conventions);
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
                var message = Resources.FormatApiController_AttributeRouteRequired(
                     actionModel.DisplayName,
                    nameof(ApiControllerAttribute));
                throw new InvalidOperationException(message);
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

        // Internal for unit testing
        internal void InferParameterBindingSources(ActionModel actionModel)
        {
            if (_modelMetadataProvider == null || _apiBehaviorOptions.SuppressInferBindingSourcesForParameters)
            {
                return;
            }
            var inferredBindingSources = new BindingSource[actionModel.Parameters.Count];

            for (var i = 0; i < actionModel.Parameters.Count; i++)
            {
                var parameter = actionModel.Parameters[i];
                var bindingSource = parameter.BindingInfo?.BindingSource;
                if (bindingSource == null)
                {
                    bindingSource = InferBindingSourceForParameter(parameter);

                    parameter.BindingInfo = parameter.BindingInfo ?? new BindingInfo();
                    parameter.BindingInfo.BindingSource = bindingSource;
                }
            }

            var fromBodyParameters = actionModel.Parameters.Where(p => p.BindingInfo.BindingSource == BindingSource.Body).ToList();
            if (fromBodyParameters.Count > 1)
            {
                var parameters = string.Join(Environment.NewLine, fromBodyParameters.Select(p => p.DisplayName));
                var message = Resources.FormatApiController_MultipleBodyParametersFound(
                    actionModel.DisplayName,
                    nameof(FromQueryAttribute),
                    nameof(FromRouteAttribute),
                    nameof(FromBodyAttribute));

                message += Environment.NewLine + parameters;
                throw new InvalidOperationException(message);
            }
        }

        // For any complex types that are bound from value providers, set the prefix
        // to the empty prefix by default. This makes binding much more predictable
        // and describable via ApiExplorer

        // internal for testing
        internal void InferBoundPropertyModelPrefixes(ControllerModel controllerModel)
        {
            foreach (var property in controllerModel.ControllerProperties)
            {
                if (property.BindingInfo != null &&
                    property.BindingInfo.BinderModelName == null &&
                    property.BindingInfo.BindingSource != null &&
                    !property.BindingInfo.BindingSource.IsGreedy)
                {
                    var metadata = _modelMetadataProvider.GetMetadataForProperty(
                        controllerModel.ControllerType,
                        property.PropertyInfo.Name);
                    if (metadata.IsComplexType && !metadata.IsCollectionType)
                    {
                        property.BindingInfo.BinderModelName = string.Empty;
                    }
                }
            }
        }

        // internal for testing
        internal void InferParameterModelPrefixes(ActionModel actionModel)
        {
            foreach (var parameter in actionModel.Parameters)
            {
                var bindingInfo = parameter.BindingInfo;
                if (bindingInfo?.BindingSource != null &&
                    bindingInfo.BinderModelName == null &&
                    !bindingInfo.BindingSource.IsGreedy &&
                    IsComplexTypeParameter(parameter))
                {
                    parameter.BindingInfo.BinderModelName = string.Empty;
                }
            }
        }

        // Internal for unit testing.
        internal BindingSource InferBindingSourceForParameter(ParameterModel parameter)
        {
            if (ParameterExistsInAnyRoute(parameter.Action, parameter.ParameterName))
            {
                return BindingSource.Path;
            }

            var bindingSource = IsComplexTypeParameter(parameter) ?
                BindingSource.Body :
                BindingSource.Query;

            return bindingSource;
        }

        internal static void DiscoverApiConvention(ActionModel actionModel, ApiConventionTypeAttribute[] apiConventionAttributes)
        {
            if (actionModel.Filters.OfType<IApiResponseMetadataProvider>().Any())
            {
                // If an action already has providers, don't discover any from conventions.
                return;
            }

            if (ApiConventionResult.TryGetApiConvention(actionModel.ActionMethod, apiConventionAttributes, out var result))
            {
                actionModel.Properties[typeof(ApiConventionResult)] = result;
            }
        }

        private bool ParameterExistsInAnyRoute(ActionModel actionModel, string parameterName)
        {
            foreach (var (route, _, _) in ActionAttributeRouteModel.GetAttributeRoutes(actionModel))
            {
                if (route == null)
                {
                    continue;
                }

                var parsedTemplate = TemplateParser.Parse(route.Template);
                if (parsedTemplate.GetParameter(parameterName) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsComplexTypeParameter(ParameterModel parameter)
        {
            // No need for information from attributes on the parameter. Just use its type.
            var metadata = _modelMetadataProvider
                .GetMetadataForType(parameter.ParameterInfo.ParameterType);
            return metadata.IsComplexType && !metadata.IsCollectionType;
        }
    }
}
