// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Constraints an action to a route key and value.
    /// </summary>
    public class RouteDataActionConstraint
    {
        private RouteDataActionConstraint(string routeKey)
        {
            if (routeKey == null)
            {
                throw new ArgumentNullException("routeKey");
            }

            RouteKey = routeKey;
        }

        /// <summary>
        /// Initializes a <see cref="RouteDataActionConstraint"/> with a key and value, that are
        /// required to make the action match.
        /// </summary>
        /// <param name="routeKey">The route key.</param>
        /// <param name="routeValue">The route value.</param>
        /// <remarks>
        /// Passing a <see cref="string.Empty"/> or <see langword="null" /> to <paramref name="routeValue"/>
        /// is a way to express that routing cannot produce a value for this key.
        /// </remarks>
        public RouteDataActionConstraint(string routeKey, string routeValue)
            : this(routeKey)
        {
            RouteValue = routeValue ?? string.Empty;

            if (string.IsNullOrEmpty(routeValue))
            {
                KeyHandling = RouteKeyHandling.DenyKey;
            }
            else
            {
                KeyHandling = RouteKeyHandling.RequireKey;
            }
        }

        /// <summary>
        /// Create a catch all constraint for the given key.
        /// </summary>
        /// <param name="routeKey">Route key.</param>
        /// <returns>a <see cref="RouteDataActionConstraint"/> that represents a catch all constraint.</returns>
        public static RouteDataActionConstraint CreateCatchAll(string routeKey)
        {
            var c = new RouteDataActionConstraint(routeKey);
            c.KeyHandling = RouteKeyHandling.CatchAll;
            c.RouteValue = string.Empty;

            return c;
        }

        /// <summary>
        /// The route key this constraint matches against.
        /// </summary>
        public string RouteKey { get; private set; }

        /// <summary>
        /// The route value this constraint matches against.
        /// </summary>
        public string RouteValue { get; private set; }

        /// <summary>
        /// The key handling definition for this constraint.
        /// </summary>
        public RouteKeyHandling KeyHandling { get; private set; }
    }
}
