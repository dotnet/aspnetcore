// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of a <see cref="IRouteConstraintProvider"/>. Logged as a substructure of
    /// <see cref="ControllerModelValues"/>
    /// </summary>
    public class RouteConstraintProviderValues : LoggerStructureBase
    {
        public RouteConstraintProviderValues([NotNull] IRouteConstraintProvider inner)
        {
            RouteKey = inner.RouteKey;
            RouteValue = inner.RouteValue;
            RouteKeyHandling = inner.RouteKeyHandling;
            BlockNonAttributedActions = inner.BlockNonAttributedActions;
        }

        /// <summary>
        /// The route value key. See <see cref="IRouteConstraintProvider.RouteKey"/>.
        /// </summary>
        public string RouteKey { get; }

        /// <summary>
        /// The expected route value. See <see cref="IRouteConstraintProvider.RouteValue"/>.
        /// </summary>
        public string RouteValue { get; }

        /// <summary>
        /// The <see cref="RouteKeyHandling"/>. See <see cref="IRouteConstraintProvider.RouteKeyHandling"/>.
        /// </summary>
        public RouteKeyHandling RouteKeyHandling { get; }

        /// <summary>
        /// See <see cref="IRouteConstraintProvider.BlockNonAttributedActions"/>.
        /// </summary>
        public bool BlockNonAttributedActions { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}
