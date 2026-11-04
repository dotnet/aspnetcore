// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RazorWebSite;

public class BackSlashExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        if (context.ActionContext is ViewContext viewContext && (string)viewContext.ViewData["back-slash"] == "true")
        {
            return new[] { $@"Views\BackSlash\{context.ViewName}.cshtml" };
        }

        return viewLocations;
    }
}
