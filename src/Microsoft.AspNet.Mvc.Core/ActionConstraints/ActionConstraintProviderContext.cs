// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Context for an action constraint provider.
    /// </summary>
    public class ActionConstraintProviderContext
    {
        /// <summary>
        /// Creates a new <see cref="ActionConstraintProviderContext"/>.
        /// </summary>
        /// <param name="action">The <see cref="ActionDescriptor"/> for which constraints are being created.</param>
        /// <param name="items">The list of <see cref="ActionConstraintItem"/> objects.</param>
        public ActionConstraintProviderContext(
            [NotNull] ActionDescriptor action, 
            [NotNull] IList<ActionConstraintItem> items)
        {
            Action = action;
            Results = items;
        }

        /// <summary>
        /// The <see cref="ActionDescriptor"/> for which constraints are being created.
        /// </summary>
        public ActionDescriptor Action { get; private set; }

        /// <summary>
        /// The list of <see cref="ActionConstraintItem"/> objects.
        /// </summary>
        public IList<ActionConstraintItem> Results { get; private set; }
    }
}