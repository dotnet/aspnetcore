// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

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
