// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Description
{
    public class ApiParameterDescription
    {
        public bool IsOptional { get; set; }

        public ModelMetadata ModelMetadata { get; set; }

        public string Name { get; set; }

        public ParameterDescriptor ParameterDescriptor { get; set; }

        public ApiParameterSource Source { get; set; }

        public IRouteConstraint Constraint { get; set; }

        public object DefaultValue { get; set; }

        public Type Type { get; set; }
    }
}