// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace FiltersWebSite
{
    [ControllerActionFilter]
    public class ActionFilterController : Controller
    {
        [ChangeContentActionFilter]
        public IActionResult GetHelloWorld(IList<ContentResult> fromGlobalActionFilter)
        {
            // Should have got content from Global Action Filter followed by Controller Override.
            if (fromGlobalActionFilter != null)
            {
                ContentResult combinedResult = null;
                var resultsFromActionFilters = fromGlobalActionFilter as List<ContentResult>;
                foreach (var result in resultsFromActionFilters)
                {
                    combinedResult = Helpers.GetContentResult(combinedResult, result.Content);
                }

                return Helpers.GetContentResult(combinedResult, "Hello World");
            }

            return null;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            object obj;
            List<ContentResult> filters;

            if (context.ActionArguments.TryGetValue("fromGlobalActionFilter", out obj))
            {
                filters = (List<ContentResult>)obj;
            }
            else
            {
                filters = new List<ContentResult>();
                context.ActionArguments.Add("fromGlobalActionFilter", filters);
            }

            filters.Add(Helpers.GetContentResult(context.Result, "Controller override - OnActionExecuting"));
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.Result = Helpers.GetContentResult(context.Result, "Controller override - OnActionExecuted");
        }
    }
}