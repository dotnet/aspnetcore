// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    /// <summary>
    /// <para>
    /// An attribute which specifies a required route value for an action or controller.
    /// </para>
    /// <para>
    /// When placed on an action, the route data of a request must match the expectations of the required route data
    /// in order for the action to be selected. All other actions without a route value for the given key cannot be
    /// selected unless the route data of the request does omits a value matching the key.
    /// See <see cref="IRouteValueProvider"/> for more details and examples.
    /// </para>
    /// <para>
    /// When placed on a controller, unless overridden by the action, the constraint applies to all
    /// actions defined by the controller.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class RouteValueAttribute : Attribute, IRouteValueProvider
    {
        /// <summary>
        /// Creates a new <see cref="RouteValueAttribute"/>.
        /// </summary>
        /// <param name="routeKey">The route value key.</param>
        /// <param name="routeValue">The expected route value.</param>
        protected RouteValueAttribute(
            string routeKey,
            string routeValue)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            if (routeValue == null)
            {
                throw new ArgumentNullException(nameof(routeValue));
            }

            RouteKey = routeKey;
            RouteValue = routeValue;
        }

        /// <inheritdoc />
        public string RouteKey { get; }

        /// <inheritdoc />
        public string RouteValue { get; }
    }
}
