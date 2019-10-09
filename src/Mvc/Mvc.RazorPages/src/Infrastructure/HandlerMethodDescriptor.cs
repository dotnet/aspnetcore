// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class HandlerMethodDescriptor
    {
        public MethodInfo MethodInfo { get; set; }

        public string HttpMethod { get; set; }

        public string Name { get; set; }

        public IList<HandlerParameterDescriptor> Parameters { get; set; }
    }
}
