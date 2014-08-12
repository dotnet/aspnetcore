// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="IActionConstraintMetadata"/> with or without a corresponding
    /// <see cref="IActionConstraint"/>.
    /// </summary>
    public class ActionConstraintItem
    {
        /// <summary>
        /// Creates a new <see cref="ActionConstraintItem"/>.
        /// </summary>
        /// <param name="metadata">The <see cref="IActionConstraintMetadata"/> instance.</param>
        public ActionConstraintItem([NotNull] IActionConstraintMetadata metadata)
        {
            Metadata = metadata;
        }

        /// <summary>
        /// The <see cref="IActionConstraint"/> associated with <see cref="Metadata"/>.
        /// </summary>
        public IActionConstraint Constraint { get; set; }

        /// <summary>
        /// The <see cref="IActionConstraintMetadata"/> instance.
        /// </summary>
        public IActionConstraintMetadata Metadata { get; private set; }
    }
}