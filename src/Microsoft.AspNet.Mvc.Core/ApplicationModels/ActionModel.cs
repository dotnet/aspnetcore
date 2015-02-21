// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    [DebuggerDisplay("Name={ActionName}({Methods()}), Type={Controller.ControllerType.Name}," +
                     " Route: {AttributeRouteModel?.Template}, Filters: {Filters.Count}")]
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
            Properties = new Dictionary<object, object>();
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
            Properties = new Dictionary<object, object>(other.Properties);

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
        /// <see cref="ActionModel.ApiExplorer"/> allows configuration of settings for ApiExplorer
        /// which apply to the action.
        /// 
        /// Settings applied by <see cref="ActionModel.ApiExplorer"/> override settings from
        /// <see cref="ApplicationModel.ApiExplorer"/> and <see cref="ControllerModel.ApiExplorer"/>.
        /// </remarks>
        public ApiExplorerModel ApiExplorer { get; set; }

        public AttributeRouteModel AttributeRouteModel { get; set; }

        public IReadOnlyList<object> Attributes { get; }

        public ControllerModel Controller { get; set; }

        public IList<IFilter> Filters { get; private set; }

        public IList<string> HttpMethods { get; private set; }

        public IList<ParameterModel> Parameters { get; private set; }

        public IList<IRouteConstraintProvider> RouteConstraints { get; private set; }

        /// <summary>
        /// Gets a set of properties associated with the action.
        /// These properties will be copied to <see cref="ActionDescriptor.Properties"/>.
        /// </summary>
        /// <remarks>
        /// Entries will take precedence over entries with the same key in
        /// <see cref="ApplicationModel.Properties"/> and <see cref="ControllerModel.Properties"/>.
        /// </remarks>
        public IDictionary<object, object> Properties { get; }

        private string Methods()
        {
            if (HttpMethods.Count == 0)
            {
                return "All";
            }

            return string.Join(", ", HttpMethods);
        }
    }
}
