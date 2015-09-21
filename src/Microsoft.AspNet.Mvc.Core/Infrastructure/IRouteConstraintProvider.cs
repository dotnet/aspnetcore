// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.Infrastructure
{
    /// <summary>
    /// An interface for metadata which provides <see cref="RouteDataActionConstraint"/> values
    /// for a controller or action.
    /// </summary>
    public interface IRouteConstraintProvider
    {
        /// <summary>
        /// The route value key.
        /// </summary>
        string RouteKey { get; }

        /// <summary>
        /// The <see cref="RouteKeyHandling"/>.
        /// </summary>
        RouteKeyHandling RouteKeyHandling { get; }

        /// <summary>
        /// The expected route value. Will be null unless <see cref="RouteKeyHandling"/> is
        /// set to <see cref="RouteKeyHandling.RequireKey"/>.
        /// </summary>
        string RouteValue { get; }

        /// <summary>
        /// Set to true to negate this constraint on all actions that do not define a behavior for this route key.
        /// </summary>
        bool BlockNonAttributedActions { get; }
    }
}
