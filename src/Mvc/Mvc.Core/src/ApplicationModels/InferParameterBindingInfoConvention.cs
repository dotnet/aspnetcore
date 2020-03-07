// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An <see cref="IActionModelConvention"/> that infers <see cref="BindingInfo.BindingSource"/> for parameters.
    /// </summary>
    /// <remarks>
    /// The goal of this convention is to make intuitive and easy to document <see cref="BindingSource"/> inferences. The rules are:
    /// <list type="number">
    /// <item>A previously specified <see cref="BindingInfo.BindingSource" /> is never overwritten.</item>
    /// <item>A complex type parameter (<see cref="ModelMetadata.IsComplexType"/>) is assigned <see cref="BindingSource.Body"/>.</item>
    /// <item>Parameter with a name that appears as a route value in ANY route template is assigned <see cref="BindingSource.Path"/>.</item>
    /// <item>All other parameters are <see cref="BindingSource.Query"/>.</item>
    /// </list>
    /// </remarks>
    public class InferParameterBindingInfoConvention : IActionModelConvention
    {
        private readonly IModelMetadataProvider _modelMetadataProvider;

        public InferParameterBindingInfoConvention(
            IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadataProvider = modelMetadataProvider ?? throw new ArgumentNullException(nameof(modelMetadataProvider));
        }

        protected virtual bool ShouldApply(ActionModel action) => true;

        public void Apply(ActionModel action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!ShouldApply(action))
            {
                return;
            }

            InferParameterBindingSources(action);
        }

        internal void InferParameterBindingSources(ActionModel action)
        {
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
            if (IsComplexTypeParameter(parameter))
            {
                return BindingSource.Body;
            }

            if (ParameterExistsInAnyRoute(parameter.Action, parameter.ParameterName))
            {
                return BindingSource.Path;
            }

            return BindingSource.Query;
        }

        private bool ParameterExistsInAnyRoute(ActionModel action, string parameterName)
        {
            foreach (var selector in ActionAttributeRouteModel.FlattenSelectors(action))
            {
                if (selector.AttributeRouteModel == null)
                {
                    continue;
                }

                var parsedTemplate = TemplateParser.Parse(selector.AttributeRouteModel.Template);
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
            var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterInfo.ParameterType);

            return metadata.IsComplexType;
        }
    }
}
