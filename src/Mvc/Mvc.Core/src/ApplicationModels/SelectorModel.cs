// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    public class SelectorModel
    {
        public SelectorModel()
        {
            ActionConstraints = new List<IActionConstraintMetadata>();
            EndpointMetadata = new List<object>();
        }

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

        public AttributeRouteModel AttributeRouteModel { get; set; }

        public IList<IActionConstraintMetadata> ActionConstraints { get; }

        public IList<object> EndpointMetadata { get; }
    }
}
