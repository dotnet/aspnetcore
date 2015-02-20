// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor;

namespace RazorWebSite
{
    public class CustomPartialDirectoryViewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public virtual IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
                                                               IEnumerable<string> viewLocations)
        {
            if (context.IsPartial)
            {
                return ExpandViewLocationsCore(viewLocations);
            }

            return viewLocations;
        }

        private IEnumerable<string> ExpandViewLocationsCore(IEnumerable<string> viewLocations)
        {
            foreach (var location in viewLocations)
            {
                yield return "/Shared-Views/{1}/{0}.cshtml";
                yield return location;
            }
        }
    }
}