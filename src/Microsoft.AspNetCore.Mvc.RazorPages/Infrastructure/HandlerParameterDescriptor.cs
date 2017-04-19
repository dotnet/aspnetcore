// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class HandlerParameterDescriptor : ParameterDescriptor
    {
        public object DefaultValue { get; set; }

        public ParameterInfo Parameter { get; set; }
    }
}
