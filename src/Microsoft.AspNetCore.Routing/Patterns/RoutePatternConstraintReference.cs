// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Routing.Matchers;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// The parsed representation of a constraint in a <see cref="RoutePattern"/> parameter.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString()}")]
    public sealed class RoutePatternConstraintReference
    {
        internal RoutePatternConstraintReference(string content)
        {
            Content = content;
        }

        internal RoutePatternConstraintReference(IRouteConstraint constraint)
        {
            Constraint = constraint;
        }

        internal RoutePatternConstraintReference(MatchProcessor matchProcessor)
        {
            MatchProcessor = matchProcessor;
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
        /// Gets a pre-existing <see cref="Matchers.MatchProcessor"/> that was used to construct this reference.
        /// </summary>
        public MatchProcessor MatchProcessor { get; }

        private string DebuggerToString()
        {
            return Content;
        }
    }
}