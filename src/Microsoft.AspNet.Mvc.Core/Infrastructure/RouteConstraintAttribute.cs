// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    /// <summary>
    /// An attribute which specifies a required route value for an action or controller.
    ///
    /// When placed on an action, the route data of a request must match the expectations of the route
    /// constraint in order for the action to be selected. See <see cref="RouteKeyHandling"/> for
    /// the expectations that must be satisfied by the route data.
    ///
    /// When placed on a controller, unless overridden by the action, the constraint applies to all
    /// actions defined by the controller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class RouteConstraintAttribute : Attribute, IRouteConstraintProvider
    {
        /// <summary>
        /// Creates a new <see cref="RouteConstraintAttribute"/> with <see cref="RouteKeyHandling"/> set as
        /// <see cref="RouteKeyHandling.DenyKey"/>.
        /// </summary>
        /// <param name="routeKey">The route value key.</param>
        protected RouteConstraintAttribute(string routeKey)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException(nameof(routeKey));
            }

            RouteKey = routeKey;
            RouteKeyHandling = RouteKeyHandling.DenyKey;
        }

        /// <summary>
        /// Creates a new <see cref="RouteConstraintAttribute"/> with
        /// <see cref="RouteConstraintAttribute.RouteKeyHandling"/> set to <see cref="RouteKeyHandling.RequireKey"/>.
        /// </summary>
        /// <param name="routeKey">The route value key.</param>
        /// <param name="routeValue">The expected route value.</param>
        /// <param name="blockNonAttributedActions">
        /// Set to true to negate this constraint on all actions that do not define a behavior for this route key.
        /// </param>
        protected RouteConstraintAttribute(
            string routeKey,
            string routeValue,
            bool blockNonAttributedActions)
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
            BlockNonAttributedActions = blockNonAttributedActions;
        }

        /// <inheritdoc />
        public string RouteKey { get; private set; }

        /// <inheritdoc />
        public RouteKeyHandling RouteKeyHandling { get; private set; }

        /// <inheritdoc />
        public string RouteValue { get; private set; }

        /// <inheritdoc />
        public bool BlockNonAttributedActions { get; private set; }
    }
}
