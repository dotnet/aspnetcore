// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    /// <summary>
    /// A candidate action for action selection.
    /// </summary>
    public struct ActionSelectorCandidate
    {
        /// <summary>
        /// Creates a new <see cref="ActionSelectorCandidate"/>.
        /// </summary>
        /// <param name="action">The <see cref="ActionDescriptor"/> representing a candidate for selection.</param>
        /// <param name="constraints">
        /// The list of <see cref="IActionConstraint"/> instances associated with <paramref name="action"/>.
        /// </param>
        public ActionSelectorCandidate(ActionDescriptor action, IReadOnlyList<IActionConstraint> constraints)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Action = action;
            Constraints = constraints;
        }

        /// <summary>
        /// The <see cref="ActionDescriptor"/> representing a candidate for selection.
        /// </summary>
        public ActionDescriptor Action { get; }

        /// <summary>
        /// The list of <see cref="IActionConstraint"/> instances associated with <see name="Action"/>.
        /// </summary>
        public IReadOnlyList<IActionConstraint> Constraints { get; }
    }
}