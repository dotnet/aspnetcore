// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Razor;

namespace MvcSubAreaSample.Web
{
    public class SubAreaViewLocationExpander : IViewLocationExpander
    {
        private const string _subAreaKey = "subarea";

        public IEnumerable<string> ExpandViewLocations(
            ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            if (context.Values.ContainsKey(_subAreaKey))
            {
                var subArea = RazorViewEngine.GetNormalizedRouteValue(context.ActionContext, _subAreaKey);
                var subareaViewLocations = new string[]
                {
                    "/Areas/{2}/Areas/" + subArea + "/Views/{1}/{0}.cshtml"
                };
                viewLocations = subareaViewLocations.Concat(viewLocations);
            }
            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var subArea = context.ActionContext.ActionDescriptor.RouteConstraints.FirstOrDefault(
                s => s.RouteKey == "subarea" && !string.IsNullOrEmpty(s.RouteValue));

            if (subArea != null)
            {
                context.Values[_subAreaKey] = subArea.RouteValue;
            }
        }
    }
}
