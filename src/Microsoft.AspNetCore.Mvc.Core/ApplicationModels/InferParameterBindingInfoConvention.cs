// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A <see cref="IControllerModelConvention"/> that
    /// <list type="bullet">
    /// <item>infers binding sources for parameters</item>
    /// <item><see cref="BindingInfo.BinderModelName"/> for bound properties and parameters.</item>
    /// </list>
    /// </summary>
    public class InferParameterBindingInfoConvention : IControllerModelConvention
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;

        public InferParameterBindingInfoConvention(
            IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadataProvider = modelMetadataProvider ?? throw new ArgumentNullException(nameof(modelMetadataProvider));
        }

        /// <summary>
        /// Gets or sets a value that determines if model binding sources are inferred for action parameters on controllers is suppressed.
        /// </summary>
        public bool SuppressInferBindingSourcesForParameters { get; set; }

        protected virtual bool ShouldApply(ControllerModel controller) => true;

        public void Apply(ControllerModel controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (!ShouldApply(controller))
            {
                return;
            }

            InferBoundPropertyModelPrefixes(controller);

            foreach (var action in controller.Actions)
            {
                InferParameterBindingSources(action);
                InferParameterModelPrefixes(action);
            }
        }

        internal void InferParameterBindingSources(ActionModel action)
        {
            if (SuppressInferBindingSourcesForParameters)
            {
                return;
            }

            for (var i = 0; i < action.Parameters.Count; i++)
            {
                var parameter = action.Parameters[i];
                var bindingSource = parameter.BindingInfo?.BindingSource;
                if (bindingSource == null)
                {
                    bindingSource = InferBindingSourceForParameter(parameter);

                    parameter.BindingInfo = parameter.BindingInfo ?? new BindingInfo();
                    parameter.BindingInfo.BindingSource = bindingSource;
                }
            }

            var fromBodyParameters = action.Parameters.Where(p => p.BindingInfo.BindingSource == BindingSource.Body).ToList();
            if (fromBodyParameters.Count > 1)
            {
                var parameters = string.Join(Environment.NewLine, fromBodyParameters.Select(p => p.DisplayName));
                var message = Resources.FormatApiController_MultipleBodyParametersFound(
                    action.DisplayName,
                    nameof(FromQueryAttribute),
                    nameof(FromRouteAttribute),
                    nameof(FromBodyAttribute));

                message += Environment.NewLine + parameters;
                throw new InvalidOperationException(message);
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

        // For any complex types that are bound from value providers, set the prefix
        // to the empty prefix by default. This makes binding much more predictable
        // and describable via ApiExplorer
        internal void InferBoundPropertyModelPrefixes(ControllerModel controllerModel)
        {
            foreach (var property in controllerModel.ControllerProperties)
            {
                if (property.BindingInfo != null &&
                    property.BindingInfo.BinderModelName == null &&
                    property.BindingInfo.BindingSource != null &&
                    !property.BindingInfo.BindingSource.IsGreedy &&
                    !IsFormFile(property.ParameterType))
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

        internal void InferParameterModelPrefixes(ActionModel action)
        {
            foreach (var parameter in action.Parameters)
            {
                var bindingInfo = parameter.BindingInfo;
                if (bindingInfo?.BindingSource != null &&
                    bindingInfo.BinderModelName == null &&
                    !bindingInfo.BindingSource.IsGreedy &&
                    !IsFormFile(parameter.ParameterType) &&
                    IsComplexTypeParameter(parameter))
                {
                    parameter.BindingInfo.BinderModelName = string.Empty;
                }
            }
        }

        private bool ParameterExistsInAnyRoute(ActionModel action, string parameterName)
        {
            foreach (var (route, _, _) in ActionAttributeRouteModel.GetAttributeRoutes(action))
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

        private static bool IsFormFile(Type parameterType)
        {
            return typeof(IFormFile).IsAssignableFrom(parameterType) ||
                typeof(IEnumerable<IFormFile>).IsAssignableFrom(parameterType);
        }
    }
}
