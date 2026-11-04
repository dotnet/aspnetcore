// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor;

namespace RazorWebSite;

public class NonMainPageViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context)
    {
    }

    public virtual IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        if (context.IsMainPage)
        {
            return viewLocations;
        }

        return ExpandViewLocationsCore(viewLocations);
    }

    private IEnumerable<string> ExpandViewLocationsCore(IEnumerable<string> viewLocations)
    {
        yield return "/Shared-Views/{1}/{0}.cshtml";

        foreach (var location in viewLocations)
        {
            yield return location;
        }
    }
}
