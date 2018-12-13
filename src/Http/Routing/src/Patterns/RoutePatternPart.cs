// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// Represents a part of a route pattern.
    /// </summary>
    public abstract class RoutePatternPart
    {
        // This class is **not** an extensibility point - every part of the routing system
        // needs to be aware of what kind of parts we support.
        //
        // It is abstract so we can add semantics later inside the library.
        private protected RoutePatternPart(RoutePatternPartKind partKind)
        {
            PartKind = partKind;
        }

        /// <summary>
        /// Gets the <see cref="RoutePatternPartKind"/> of this part.
        /// </summary>
        public RoutePatternPartKind PartKind { get; }

        /// <summary>
        /// Returns <c>true</c> if this part is literal text. Otherwise returns <c>false</c>.
        /// </summary>
        public bool IsLiteral => PartKind == RoutePatternPartKind.Literal;

        /// <summary>
        /// Returns <c>true</c> if this part is a route parameter. Otherwise returns <c>false</c>.
        /// </summary>
        public bool IsParameter => PartKind == RoutePatternPartKind.Parameter;

        /// <summary>
        /// Returns <c>true</c> if this part is an optional separator. Otherwise returns <c>false</c>.
        /// </summary>
        public bool IsSeparator => PartKind == RoutePatternPartKind.Separator;

        internal abstract string DebuggerToString();
    }
}
