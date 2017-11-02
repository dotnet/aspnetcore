// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Constrains a dispatcher value by several child constraints.
    /// </summary>
    public class CompositeDispatcherValueConstraint : IDispatcherValueConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeDispatcherValueConstraint" /> class.
        /// </summary>
        /// <param name="constraints">The child constraints that must match for this constraint to match.</param>
        public CompositeDispatcherValueConstraint(IEnumerable<IDispatcherValueConstraint> constraints)
        {
            if (constraints == null)
            {
                throw new ArgumentNullException(nameof(constraints));
            }

            Constraints = constraints;
        }

        /// <summary>
        /// Gets the child constraints that must match for this constraint to match.
        /// </summary>
        public IEnumerable<IDispatcherValueConstraint> Constraints { get; private set; }

        /// <inheritdoc />
        public bool Match(DispatcherValueConstraintContext constraintContext)
        {
            if (constraintContext == null)
            {
                throw new ArgumentNullException(nameof(constraintContext));
            }

            foreach (var constraint in Constraints)
            {
                if (!constraint.Match(constraintContext))
                {
                    return false;
                }
            }

            return true;
        }
    }
}