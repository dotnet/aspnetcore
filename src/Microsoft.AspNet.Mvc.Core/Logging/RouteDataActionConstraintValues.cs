// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of a <see cref="RouteDataActionConstraint"/>. Logged as a substructure of
    /// <see cref="ActionDescriptorValues"/>.
    /// </summary>
    public class RouteDataActionConstraintValues : LoggerStructureBase
    {
        public RouteDataActionConstraintValues([NotNull] RouteDataActionConstraint inner)
        {
            RouteKey = inner.RouteKey;
            RouteValue = inner.RouteValue;
            KeyHandling = inner.KeyHandling;
        }

        /// <summary>
        /// The route key. See <see cref="RouteDataActionConstraint.RouteKey"/>.
        /// </summary>
        public string RouteKey { get; }

        /// <summary>
        /// The route value. See <see cref="RouteDataActionConstraint.RouteValue"/>.
        /// </summary>
        public string RouteValue { get; }

        /// <summary>
        /// The  <see cref="RouteKeyHandling"/>. See <see cref="RouteDataActionConstraint.KeyHandling"/>.
        /// </summary>
        public RouteKeyHandling KeyHandling { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}