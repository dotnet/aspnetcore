// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ParameterDescriptor
    {
        public string Name { get; set; }

        public bool IsOptional { get; set; }

        public Type ParameterType { get; set; }

        public IBinderMetadata BinderMetadata { get; set; }
    }
}
