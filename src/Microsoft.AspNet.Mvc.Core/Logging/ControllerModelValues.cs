// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of a <see cref="ControllerModel"/>. Logged during controller discovery.
    /// </summary>
    public class ControllerModelValues : LoggerStructureBase
    {
        public ControllerModelValues(ControllerModel inner)
        {
            if (inner != null)
            {
                ControllerName = inner.ControllerName;
                ControllerType = inner.ControllerType.AsType();
                ApiExplorer = new ApiExplorerModelValues(inner.ApiExplorer);
                Actions = inner.Actions.Select(a => new ActionModelValues(a)).ToList();
                Attributes = inner.Attributes.Select(a => a.GetType()).ToList();
                Filters = inner.Filters.Select(f => new FilterValues(f)).ToList();
                ActionConstraints = inner.ActionConstraints?.Select(a => new ActionConstraintValues(a))?.ToList();
                RouteConstraints = inner.RouteConstraints.Select(
                    r => new RouteConstraintProviderValues(r)).ToList();
                AttributeRoutes = inner.AttributeRoutes.Select(
                    a => new AttributeRouteModelValues(a)).ToList();
            }
        }

        /// <summary>
        /// The name of the controller. See <see cref="ControllerModel.ControllerName"/>.
        /// </summary>
        public string ControllerName { get; }

        /// <summary>
        /// The <see cref="Type"/> of the controller. See <see cref="ControllerModel.ControllerType"/>.
        /// </summary>
        public Type ControllerType { get; }

        /// <summary>
        /// See <see cref="ControllerModel.ApiExplorer"/>.
        /// </summary>
        public ApiExplorerModelValues ApiExplorer { get; }

        /// <summary>
        /// The actions of the controller as <see cref="ActionModelValues"/>.
        /// See <see cref="ControllerModel.Actions"/>.
        /// </summary>
        public List<ActionModelValues> Actions { get; }

        /// <summary>
        /// The <see cref="Type"/>s of the controller's attributes. 
        /// See <see cref="ControllerModel.Attributes"/>.
        /// </summary>
        public List<Type> Attributes { get; }

        /// <summary>
        /// The filters on the controller as <see cref="FilterValues"/>.
        /// See <see cref="ControllerModel.Filters"/>.
        /// </summary>
        public List<FilterValues> Filters { get; }

        /// <summary>
        /// The action constraints on the controller as <see cref="ActionConstraintValues"/>.
        /// See <see cref="ControllerModel.ActionConstraints"/>.
        /// </summary>
        public List<ActionConstraintValues> ActionConstraints { get; }

        /// <summary>
        /// The route constraints on the controller as <see cref="RouteConstraintProviderValues"/>.
        /// See <see cref="ControllerModel.RouteConstraints"/>.
        /// </summary>
        public List<RouteConstraintProviderValues> RouteConstraints { get; set; }

        /// <summary>
        /// The attribute routes on the controller as <see cref="AttributeRouteModelValues"/>.
        /// See <see cref="ControllerModel.AttributeRoutes"/>.
        /// </summary>
        public List<AttributeRouteModelValues> AttributeRoutes { get; set; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}