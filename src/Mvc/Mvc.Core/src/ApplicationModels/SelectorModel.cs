// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A type that represents a selector.
    /// </summary>
    public class SelectorModel
    {
        /// <summary>
        /// Intializes a new <see cref="SelectorModel"/>.
        /// </summary>
        public SelectorModel()
        {
            ActionConstraints = new List<IActionConstraintMetadata>();
            EndpointMetadata = new List<object>();
        }

        /// <summary>
        /// Intializes a new <see cref="SelectorModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="SelectorModel"/> to copy from.</param>
        public SelectorModel(SelectorModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ActionConstraints = new List<IActionConstraintMetadata>(other.ActionConstraints);
            EndpointMetadata = new List<object>(other.EndpointMetadata);

            if (other.AttributeRouteModel != null)
            {
                AttributeRouteModel = new AttributeRouteModel(other.AttributeRouteModel);
            }
        }

        /// <summary>
        /// The <see cref="AttributeRouteModel"/>.
        /// </summary>
        public AttributeRouteModel? AttributeRouteModel { get; set; }

        /// <summary>
        /// The list of <see cref="IActionConstraintMetadata"/>.
        /// </summary>
        public IList<IActionConstraintMetadata> ActionConstraints { get; }

        /// <summary>
        /// Gets the <see cref="EndpointMetadata"/> associated with the <see cref="SelectorModel"/>.
        /// </summary>
        public IList<object> EndpointMetadata { get; }
    }
}
