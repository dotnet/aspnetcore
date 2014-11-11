// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Routing.Template
{
    /// <summary>
    /// The parsed representation of an inline constraint in a route parameter.
    /// </summary>
    public class InlineConstraint
    {
        /// <summary>
        /// Creates a new <see cref="InlineConstraint"/>.
        /// </summary>
        /// <param name="constraint">The constraint text.</param>
        public InlineConstraint([NotNull] string constraint)
        {
            Constraint = constraint;
        }

        /// <summary>
        /// Gets the constraint text.
        /// </summary>
        public string Constraint { get; }
    }
}