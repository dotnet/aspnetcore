// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageBoundPropertyDescriptor : ParameterDescriptor
    {
        public PropertyInfo Property { get; set; }
    }
}
