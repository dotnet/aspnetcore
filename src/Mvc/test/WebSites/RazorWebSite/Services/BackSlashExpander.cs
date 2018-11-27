// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RazorWebSite
{
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
}