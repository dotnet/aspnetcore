// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
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
        /// Creates a new <see cref="RouteConstraintAttribute"/>.
        /// </summary>
        /// <param name="routeKey">The route value key.</param>
        /// <param name="keyHandling">
        /// The <see cref="RouteKeyHandling"/> value. Must be <see cref="RouteKeyHandling.CatchAll "/>
        /// or <see cref="RouteKeyHandling.DenyKey"/>.
        /// </param>
        protected RouteConstraintAttribute(
            [NotNull] string routeKey,
            RouteKeyHandling keyHandling)
        {
            RouteKey = routeKey;
            RouteKeyHandling = keyHandling;

            if (keyHandling != RouteKeyHandling.CatchAll &&
                keyHandling != RouteKeyHandling.DenyKey)
            {
                var message = Resources.FormatRouteConstraintAttribute_InvalidKeyHandlingValue(
                    Enum.GetName(typeof(RouteKeyHandling), RouteKeyHandling.CatchAll),
                    Enum.GetName(typeof(RouteKeyHandling), RouteKeyHandling.DenyKey));
                throw new ArgumentException(message, "keyHandling");
            }
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
            [NotNull]string routeKey,
            [NotNull]string routeValue,
            bool blockNonAttributedActions)
        {
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
