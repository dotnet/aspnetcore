// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Server
{
    public class ComponentPrerrenderingContext
    {
        public Type ComponentType { get; set; }
        public ParameterCollection Parameters { get; set; }
        public HttpContext Context { get; set; }
    }
}
