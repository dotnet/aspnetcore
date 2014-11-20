// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNet.Routing.Logging
{
    /// <summary>
    /// Describes the state of <see cref="RouteConstraintMatcher.Match"/>.
    /// </summary>
    public class RouteConstraintMatcherMatchValues
    {
        /// <summary>
        /// The name of the state.
        /// </summary>
        public string Name
        {
            get
            {
                return "RouteConstraintMatcher.Match";
            }
        }

        /// <summary>
        /// The key of the constraint.
        /// </summary>
        public string ConstraintKey { get; set; }

        /// <summary>
        /// The constraint.
        /// </summary>
        public IRouteConstraint Constraint { get; set; }

        /// <summary>
        /// True if the <see cref="Constraint"/> matched.
        /// </summary>
        public bool Matched { get; set; }

        /// <summary>
        /// A summary of the data for display.
        /// </summary>
        public string Summary
        {
            get
            {
                var builder = new StringBuilder();
                builder.AppendLine(Name);
                builder.Append("\tConstraint key: ");
                builder.AppendLine(ConstraintKey);
                builder.Append("\tConstraint: ");
                builder.Append(Constraint);
                builder.AppendLine();
                builder.Append("\tMatched? ");
                builder.Append(Matched);
                return builder.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Summary;
        }
    }
}