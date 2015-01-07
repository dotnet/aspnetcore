// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of an <see cref="ActionDescriptor"/> or 
    /// <see cref="ControllerActionDescriptor"/>. Logged during action discovery.
    /// </summary>
    public class ActionDescriptorValues : LoggerStructureBase
    {
        public ActionDescriptorValues([NotNull] ActionDescriptor inner)
        {
            Name = inner.Name;
            DisplayName = inner.DisplayName;
            Parameters = inner.Parameters.Select(p => new ParameterDescriptorValues(p)).ToList();
            FilterDescriptors = inner.FilterDescriptors.Select(f => new FilterDescriptorValues(f)).ToList();
            RouteConstraints = inner.RouteConstraints.Select(r => new RouteDataActionConstraintValues(r)).ToList();
            AttributeRouteInfo = new AttributeRouteInfoValues(inner.AttributeRouteInfo);
            RouteValueDefaults = inner.RouteValueDefaults.ToDictionary(i => i.Key, i => i.Value.ToString());
            ActionConstraints = inner.ActionConstraints?.Select(a => new ActionConstraintValues(a))?.ToList();
            HttpMethods =
                inner.ActionConstraints?.OfType<HttpMethodConstraint>().SelectMany(c => c.HttpMethods).ToList();
            Properties = inner.Properties.ToDictionary(i => i.Key.ToString(), i => i.Value.GetType());
            var controllerActionDescriptor = inner as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                MethodInfo = controllerActionDescriptor.MethodInfo;
                ControllerName = controllerActionDescriptor.ControllerName;
                ControllerTypeInfo = controllerActionDescriptor.ControllerTypeInfo;
            }
        }

        /// <summary>
        /// The name of the action. See <see cref="ActionDescriptor.Name"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A friendly name for the action. See <see cref="ActionDescriptor.DisplayName"/>.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The parameters of the action as <see cref="ParameterDescriptorValues"/>.
        /// See <see cref="ActionDescriptor.Parameters"/>.
        /// </summary>
        public List<ParameterDescriptorValues> Parameters { get; }

        /// <summary>
        /// The filters of the action as <see cref="FilterDescriptorValues"/>.
        /// See <see cref="ActionDescriptor.FilterDescriptors"/>.
        /// </summary>
        public List<FilterDescriptorValues> FilterDescriptors { get; }

        /// <summary>
        /// The route constraints of the action as <see cref="RouteDataActionConstraintValues"/>.
        /// See <see cref="ActionDescriptor.RouteConstraints"/>
        /// </summary>
        public List<RouteDataActionConstraintValues> RouteConstraints { get; }

        /// <summary>
        /// The attribute route info of the action as <see cref="AttributeRouteInfoValues"/>.
        /// See <see cref="ActionDescriptor.AttributeRouteInfo"/>.
        /// </summary>
        public AttributeRouteInfoValues AttributeRouteInfo { get; }

        /// <summary>
        /// See <see cref="ActionDescriptor.RouteValueDefaults"/>.
        /// </summary>
        public Dictionary<string, string> RouteValueDefaults { get; }

        /// <summary>
        /// The action constraints of the action as <see cref="ActionConstraintValues"/>.
        /// See <see cref="ActionDescriptor.ActionConstraints"/>.
        /// </summary>
        public List<ActionConstraintValues> ActionConstraints { get; }

        /// <summary>
        /// The http methods this action supports.
        /// </summary>
        public List<string> HttpMethods { get; }

        /// <summary>
        /// See <see cref="ActionDescriptor.Properties"/>.
        /// </summary>
        public Dictionary<string, Type> Properties { get; }

        /// <summary>
        /// The method info of the action if this is a <see cref="ControllerActionDescriptor"/>.
        /// See <see cref="ControllerActionDescriptor.MethodInfo"/>.
        /// </summary>
        public MethodInfo MethodInfo { get; }

        /// <summary>
        /// The name of the action's controller if this is a <see cref="ControllerActionDescriptor"/>.
        /// See <see cref="ControllerActionDescriptor.ControllerName"/>.
        /// </summary>
        public string ControllerName { get; }

        /// <summary>
        /// The type info of the action's controller if this is a <see cref="ControllerActionDescriptor"/>.
        /// See <see cref="ControllerActionDescriptor.ControllerTypeInfo"/>.
        /// </summary>
        public TypeInfo ControllerTypeInfo { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}