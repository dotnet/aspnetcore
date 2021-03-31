// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
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
