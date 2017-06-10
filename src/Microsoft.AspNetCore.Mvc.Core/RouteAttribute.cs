// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies an attribute route on a controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteAttribute : Attribute, IRouteTemplateProvider
    {
        private int? _order;

        /// <summary>
        /// Creates a new <see cref="RouteAttribute"/> with the given route template.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public RouteAttribute(string template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Template = template;
        }

        /// <inheritdoc />
        public string Template { get; }

        /// <summary>
        /// Gets the route order. The order determines the order of route execution. Routes with a lower order
        /// value are tried first. If an action defines a route by providing an <see cref="IRouteTemplateProvider"/>
        /// with a non <c>null</c> order, that order is used instead of this value. If neither the action nor the
        /// controller defines an order, a default value of 0 is used.
        /// </summary>
        public int Order
        {
            get { return _order ?? 0; }
            set { _order = value; }
        }

        /// <inheritdoc />
        int? IRouteTemplateProvider.Order => _order;

        /// <inheritdoc />
        public string Name { get; set; }
    }
}