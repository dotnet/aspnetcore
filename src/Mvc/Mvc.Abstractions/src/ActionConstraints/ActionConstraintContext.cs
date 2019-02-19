// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    /// <summary>
    /// Context for <see cref="IActionConstraint"/> execution.
    /// </summary>
    public class ActionConstraintContext
    {
        /// <summary>
        /// The list of <see cref="ActionSelectorCandidate"/>. This includes all actions that are valid for the current
        /// request, as well as their constraints.
        /// </summary>
        public IReadOnlyList<ActionSelectorCandidate> Candidates { get; set; }

        /// <summary>
        /// The current <see cref="ActionSelectorCandidate"/>.
        /// </summary>
        public ActionSelectorCandidate CurrentCandidate { get; set; }

        /// <summary>
        /// The <see cref="RouteContext"/>.
        /// </summary>
        public RouteContext RouteContext { get; set; }
    }
}