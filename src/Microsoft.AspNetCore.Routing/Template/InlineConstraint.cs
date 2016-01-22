// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Template
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
        public InlineConstraint(string constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException(nameof(constraint));
            }

            Constraint = constraint;
        }

        /// <summary>
        /// Gets the constraint text.
        /// </summary>
        public string Constraint { get; }
    }
}