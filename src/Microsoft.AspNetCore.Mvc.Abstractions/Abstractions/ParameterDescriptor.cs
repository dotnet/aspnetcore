// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Abstractions
{
    public class ParameterDescriptor
    {
        public string Name { get; set; }

        public Type ParameterType { get; set; }

        public BindingInfo BindingInfo { get; set; }
    }
}
