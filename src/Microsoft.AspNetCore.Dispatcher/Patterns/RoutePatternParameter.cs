// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class RoutePatternParameter : RoutePatternPart
    {
        internal RoutePatternParameter(
            string rawText,
            string name,
            object defaultValue,
            RoutePatternParameterKind parameterKind,
            ConstraintReference[] constraints)
        {
            // See #475 - this code should have some asserts, but it can't because of the design of InlineRouteParameterParser.

            RawText = rawText;
            Name = name;
            DefaultValue = defaultValue;
            ParameterKind = parameterKind;
            Constraints = constraints;

            PartKind = RoutePatternPartKind.Parameter;
        }

        public IReadOnlyList<ConstraintReference> Constraints { get; }

        public object DefaultValue { get; }

        public bool IsCatchAll => ParameterKind == RoutePatternParameterKind.CatchAll;

        public bool IsOptional => ParameterKind == RoutePatternParameterKind.Optional;

        public RoutePatternParameterKind ParameterKind { get; }

        public override RoutePatternPartKind PartKind { get; }

        public string Name { get; }

        public override string RawText { get; }

        internal override string DebuggerToString()
        {
            return RawText ?? "{" + (IsCatchAll ? "*" : string.Empty) + Name + (IsOptional ? "?" : string.Empty) + "}";
        }
    }
}
