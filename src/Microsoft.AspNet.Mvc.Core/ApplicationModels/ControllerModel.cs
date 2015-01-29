// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class ControllerModel
    {
        public ControllerModel([NotNull] TypeInfo controllerType,
                               [NotNull] IReadOnlyList<object> attributes)
        {
            ControllerType = controllerType;

            Actions = new List<ActionModel>();
            ApiExplorer = new ApiExplorerModel();
            Attributes = new List<object>(attributes);
            AttributeRoutes = new List<AttributeRouteModel>();
            ActionConstraints = new List<IActionConstraintMetadata>();
            Filters = new List<IFilter>();
            RouteConstraints = new List<IRouteConstraintProvider>();
        }

        public ControllerModel([NotNull] ControllerModel other)
        {
            ControllerName = other.ControllerName;
            ControllerType = other.ControllerType;

            // Still part of the same application
            Application = other.Application;

            // These are just metadata, safe to create new collections
            ActionConstraints = new List<IActionConstraintMetadata>(other.ActionConstraints);
            Attributes = new List<object>(other.Attributes);
            Filters = new List<IFilter>(other.Filters);
            RouteConstraints = new List<IRouteConstraintProvider>(other.RouteConstraints);

            // Make a deep copy of other 'model' types.
            Actions = new List<ActionModel>(other.Actions.Select(a => new ActionModel(a)));
            ApiExplorer = new ApiExplorerModel(other.ApiExplorer);
            AttributeRoutes = new List<AttributeRouteModel>(
                other.AttributeRoutes.Select(a => new AttributeRouteModel(a)));
        }

        public IList<IActionConstraintMetadata> ActionConstraints { get; private set; }

        public IList<ActionModel> Actions { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="ApiExplorerModel"/> for this controller.
        /// </summary>
        public ApiExplorerModel ApiExplorer { get; set; }

        public ApplicationModel Application { get; set; }

        public IList<AttributeRouteModel> AttributeRoutes { get; private set; }

        public IReadOnlyList<object> Attributes { get; }

        public string ControllerName { get; set; }

        public TypeInfo ControllerType { get; private set; }

        public IList<IFilter> Filters { get; private set; }

        public IList<IRouteConstraintProvider> RouteConstraints { get; private set; }
    }
}
