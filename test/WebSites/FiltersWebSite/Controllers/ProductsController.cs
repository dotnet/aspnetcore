// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using System.Linq;

namespace FiltersWebSite
{
    // This controller will list the filters that are configured for each action in a header.
    // This exercises the merging of filters with the global filters collection.
    [PassThroughActionFilter(Order = -2)]
    [PassThroughResultFilter]
    public class ProductsController : Controller
    {
        [PassThroughActionFilter]
        public decimal GetPrice(int id)
        {
            return 19.95m;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Log the filter names in a header
            context.HttpContext.Response.Headers.Add(
                "filters",
                context.Filters.Select(f => f.GetType().FullName).ToArray());
        }
    }
}