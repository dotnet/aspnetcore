// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// Defines the kinds of <see cref="RoutePatternPart"/> instances.
    /// </summary>
    public enum RoutePatternPartKind
    {
        /// <summary>
        /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternLiteralPart"/>.
        /// </summary>
        Literal,

        /// <summary>
        /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternParameterPart"/>.
        /// </summary>
        Parameter,

        /// <summary>
        /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternSeparatorPart"/>.
        /// </summary>
        Separator,
    }
}
