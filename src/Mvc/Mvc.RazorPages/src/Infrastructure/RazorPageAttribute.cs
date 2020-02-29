// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    [Obsolete("This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
    public class RazorPageAttribute : RazorViewAttribute
    {
        public RazorPageAttribute(string path, Type viewType, string routeTemplate)
            : base(path, viewType)
        {
            RouteTemplate = routeTemplate;
        }

        public string RouteTemplate { get; }
    }
}
