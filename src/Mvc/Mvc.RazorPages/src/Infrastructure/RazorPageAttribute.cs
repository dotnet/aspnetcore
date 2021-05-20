// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// Obsolete: This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.
    /// </summary>
    [Obsolete("This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.")]
    public class RazorPageAttribute : RazorViewAttribute
    {
        /// <summary>
        /// This attribute has been superseded by RazorCompiledItem and will not be used by the runtime.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="viewType"></param>
        /// <param name="routeTemplate"></param>
        public RazorPageAttribute(string path, Type viewType, string routeTemplate)
            : base(path, viewType)
        {
            RouteTemplate = routeTemplate;
        }

        /// <summary>
        /// The route template.
        /// </summary>
        public string RouteTemplate { get; }
    }
}
