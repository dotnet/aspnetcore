// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing
{
    public class ControllerActionEndpointModel : EndpointModel
    {
        public ControllerActionEndpointModel(
            Type controllerType,
            string controllerName, 
            MethodInfo actionMethod, 
            string actionName)
        {
            if (controllerType == null)
            {
                throw new ArgumentNullException(nameof(controllerType));
            }

            if (string.IsNullOrEmpty(controllerName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(controllerName));
            }

            if (actionMethod == null)
            {
                throw new ArgumentNullException(nameof(actionMethod));
            }

            if (string.IsNullOrEmpty(actionName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(actionName));
            }

            ControllerType = controllerType;
            ControllerName = controllerName;
            ActionMethod = actionMethod;
            ActionName = actionName;

            Filters = new List<FilterDescriptor>();
            Parameters = new List<ControllerActionParameterModel>();
            Properties = new Dictionary<object, object>();
        }

        public ControllerActionEndpointModel(ControllerActionEndpointModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ActionName = other.ActionName;
            ActionMethod = other.ActionMethod;
            ControllerName = other.ControllerName;
            ControllerType = other.ControllerType;
            DisplayName = other.DisplayName;
            Order = other.Order;
            RequestDelegate = other.RequestDelegate;
            RoutePattern = other.RoutePattern;

            Filters = new List<FilterDescriptor>(other.Filters);
            Parameters = new List<ControllerActionParameterModel>(other.Parameters.Select(p => new ControllerActionParameterModel(p)));
            Properties = new Dictionary<object, object>(other.Properties);

            for (var i = 0; i < other.Metadata.Count; i++)
            {
                Metadata.Add(other.Metadata);
            }
        }

        public Type ControllerType { get; }

        public MethodInfo ActionMethod { get; }

        public string ActionName { get; }

        public string ControllerName { get; }

        public IList<FilterDescriptor> Filters { get; }

        public int Order { get; set; }

        public ICollection<ControllerActionParameterModel> Parameters { get; }

        public IDictionary<object, object> Properties { get; }

        public RoutePattern RoutePattern { get; set; }

        public override Endpoint Build()
        {
            // Filters need to be a separate collection from metadata because
            // we need to store them as descriptors. Since filters are special
            // in MVC, filters will always take precedence over things
            // put in the metadata collection.
            var filters = Filters.ToArray();
            Array.Sort(filters, FilterDescriptorOrderComparer.Comparer);

            var metadata = Metadata.ToList();
            for (var i = 0; i < filters.Length; i++)
            {
                if (!metadata.Contains(filters[i].Filter))
                {
                    metadata.Add(filters[i].Filter);
                }
            }

            var boundProperties = Parameters
                .Where(p => p.Property != null)
                .Select(p =>
                {
                    return new ControllerBoundPropertyDescriptor()
                    {
                        BindingInfo = p.BindingInfo,
                        Name = p.Name,
                        ParameterType = p.ParameterType,
                        PropertyInfo = p.Property,
                    };
                })
                .ToArray();

            var boundParameters = Parameters
                .Where(p => p.Parameter != null)
                .Select(p =>
                {
                    return new ControllerParameterDescriptor()
                    {
                        BindingInfo = p.BindingInfo,
                        Name = p.Name,
                        ParameterType = p.ParameterType,
                        ParameterInfo = p.Parameter,
                    };
                })
                .ToArray();

            Properties.TryGetValue(typeof(AttributeRouteInfo), out var obj);
            var attributeRouteInfo = obj as AttributeRouteInfo;

            Properties.TryGetValue(typeof(IList<IActionConstraintMetadata>), out obj);
            var actionConstraints = obj as IList<IActionConstraintMetadata>;

            var actionDescriptor = new ControllerActionDescriptor()
            {
                ActionConstraints = actionConstraints,
                ActionName = ActionName,
                AttributeRouteInfo = attributeRouteInfo,
                BoundProperties = boundProperties,
                ControllerName = ControllerName,
                ControllerTypeInfo = ControllerType.GetTypeInfo(),
                DisplayName = DisplayName,
                EndpointMetadata = metadata,
                FilterDescriptors = filters,
                MethodInfo = ActionMethod,
                Parameters = boundParameters,
                Properties = Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                RouteValues = RoutePattern.RequiredValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString()),
            };

            // Store the action descriptor - this way the ADCP can find the actions.
            metadata.Add(actionDescriptor);

            return new RouteEndpoint(RequestDelegate, RoutePattern, Order, new EndpointMetadataCollection(metadata), DisplayName);
        }
    }
}
