// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// The parsed representation of a constraint in a <see cref="RoutePattern"/> parameter.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternConstraintReference
    {
        internal RoutePatternConstraintReference(string parameterName, string content)
        {
            ParameterName = parameterName;
            Content = content;
        }

        internal RoutePatternConstraintReference(string parameterName, IRouteConstraint constraint)
        {
            ParameterName = parameterName;
            Constraint = constraint;
        }

        /// <summary>
        /// Gets the constraint text.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets a pre-existing <see cref="IRouteConstraint"/> that was used to construct this reference.
        /// </summary>
        public IRouteConstraint Constraint { get; }

        /// <summary>
        /// Gets the parameter name associated with the constraint.
        /// </summary>
        public string ParameterName { get; }

        private string DebuggerToString()
        {
            return Content;
        }
    }
}