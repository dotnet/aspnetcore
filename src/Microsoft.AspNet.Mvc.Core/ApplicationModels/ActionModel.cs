// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ActionModel
    {
        public ActionModel([NotNull] MethodInfo actionMethod,
                           [NotNull] IReadOnlyList<object> attributes)
        {
            ActionMethod = actionMethod;

            ApiExplorer = new ApiExplorerModel();
            Attributes = new List<object>(attributes);
            ActionConstraints = new List<IActionConstraintMetadata>();
            Filters = new List<IFilter>();
            HttpMethods = new List<string>();
            Parameters = new List<ParameterModel>();
            RouteConstraints = new List<IRouteConstraintProvider>();
        }

        public ActionModel([NotNull] ActionModel other)
        {
            ActionMethod = other.ActionMethod;
            ActionName = other.ActionName;

            // Not making a deep copy of the controller, this action still belongs to the same controller.
            Controller = other.Controller;

            // These are just metadata, safe to create new collections
            ActionConstraints = new List<IActionConstraintMetadata>(other.ActionConstraints);
            Attributes = new List<object>(other.Attributes);
            Filters = new List<IFilter>(other.Filters);
            HttpMethods = new List<string>(other.HttpMethods);

            // Make a deep copy of other 'model' types.
            ApiExplorer = new ApiExplorerModel(other.ApiExplorer);
            Parameters = new List<ParameterModel>(other.Parameters.Select(p => new ParameterModel(p)));
            RouteConstraints = new List<IRouteConstraintProvider>(other.RouteConstraints);

            if (other.AttributeRouteModel != null)
            {
                AttributeRouteModel = new AttributeRouteModel(other.AttributeRouteModel);
            }
        }

        public IList<IActionConstraintMetadata> ActionConstraints { get; private set; }

        public MethodInfo ActionMethod { get; }

        public string ActionName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ApiExplorerModel"/> for this action.
        /// </summary>
        /// <remarks>
        /// Setting the value of any properties on <see cref="ActionModel.ApiExplorer"/> will override any
        /// values set on the associated <see cref="ControllerModel.ApiExplorer"/>.
        /// </remarks>
        public ApiExplorerModel ApiExplorer { get; set; }

        public AttributeRouteModel AttributeRouteModel { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        public ControllerModel Controller { get; set; }

        public IList<IFilter> Filters { get; private set; }

        public IList<string> HttpMethods { get; private set; }

        public IList<ParameterModel> Parameters { get; private set; }

        public IList<IRouteConstraintProvider> RouteConstraints { get; private set; }
    }
}
