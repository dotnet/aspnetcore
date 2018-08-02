// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal abstract class MatchProcessorFactory
    {
        public abstract MatchProcessor Create(string parameterName, string value, bool optional);

        public abstract MatchProcessor Create(string parameterName, IRouteConstraint value, bool optional);

        public abstract MatchProcessor Create(string parameterName, MatchProcessor value, bool optional);

        public MatchProcessor Create(RoutePatternParameterPart parameter, RoutePatternConstraintReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            Debug.Assert(reference.MatchProcessor != null || reference.Constraint != null || reference.Content != null);

            if (reference.MatchProcessor != null)
            {
                return Create(parameter?.Name, reference.MatchProcessor, parameter?.IsOptional ?? false);
            }

            if (reference.Constraint != null)
            {
                return Create(parameter?.Name, reference.Constraint, parameter?.IsOptional ?? false);
            }

            if (reference.Content != null)
            {
                return Create(parameter?.Name, reference.Content, parameter?.IsOptional ?? false);
            }

            // Unreachable
            throw new NotSupportedException();
        }
    }
}
