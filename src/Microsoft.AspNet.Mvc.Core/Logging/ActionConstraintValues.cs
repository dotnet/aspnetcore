// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of an <see cref="IActionConstraintMetadata"/>. 
    /// </summary>
    public class ActionConstraintValues : LoggerStructureBase
    {
        public ActionConstraintValues(IActionConstraintMetadata inner)
        {
            var constraint = inner as IActionConstraint;
            if (constraint != null)
            {
                IsConstraint = true;
                Order = constraint.Order;
            }
            if (inner is IActionConstraintFactory)
            {
                IsFactory = true;
            }
            ActionConstraintMetadataType = inner.GetType();
        }

        /// <summary>
        /// The <see cref="Type"/> of this <see cref="IActionConstraintMetadata"/>.
        /// </summary>
        public Type ActionConstraintMetadataType { get; }

        /// <summary>
        /// The constraint order if this is an <see cref="IActionConstraint"/>. See
        /// <see cref="IActionConstraint.Order"/>.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Whether the action constraint is an <see cref="IActionConstraint"/>.
        /// </summary>
        public bool IsConstraint { get; }

        /// <summary>
        /// Whether the action constraint is an <see cref="IActionConstraintFactory"/>.
        /// </summary>
        public bool IsFactory { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}