// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Patterns
{
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

        public RoutePatternPartKind PartKind { get; }

        public bool IsLiteral => PartKind == RoutePatternPartKind.Literal;

        public bool IsParameter => PartKind == RoutePatternPartKind.Parameter;

        public bool IsSeparator => PartKind == RoutePatternPartKind.Separator;

        internal abstract string DebuggerToString();
    }
}
