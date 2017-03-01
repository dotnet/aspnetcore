// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class HandlerMethodDescriptor
    {
        public MethodInfo Method { get; set; }

        public Func<Page, object, Task<IActionResult>> Executor { get; set; }

        public string HttpMethod { get; set; }

        public StringSegment FormAction { get; set; }
    }
}