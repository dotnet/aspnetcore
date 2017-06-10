// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints
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
        public ActionConstraintItem(IActionConstraintMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            Metadata = metadata;
        }

        /// <summary>
        /// The <see cref="IActionConstraint"/> associated with <see cref="Metadata"/>.
        /// </summary>
        public IActionConstraint Constraint { get; set; }

        /// <summary>
        /// The <see cref="IActionConstraintMetadata"/> instance.
        /// </summary>
        public IActionConstraintMetadata Metadata { get; }

        /// <summary>
        /// Gets or sets a value indicating whether or not <see cref="Constraint"/> can be reused across requests.
        /// </summary>
        public bool IsReusable { get; set; }
    }
}