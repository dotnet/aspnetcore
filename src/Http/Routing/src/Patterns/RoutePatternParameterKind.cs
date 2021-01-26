// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// Defines the kinds of <see cref="RoutePatternParameterPart"/> instances.
    /// </summary>
    public enum RoutePatternParameterKind
    {
        /// <summary>
        /// The <see cref="RoutePatternParameterKind"/> of a standard parameter
        /// without optional or catch all behavior.
        /// </summary>
        Standard,

        /// <summary>
        /// The <see cref="RoutePatternParameterKind"/> of an optional parameter.
        /// </summary>
        Optional,

        /// <summary>
        /// The <see cref="RoutePatternParameterKind"/> of a catch-all parameter.
        /// </summary>
        CatchAll,
    }
}
