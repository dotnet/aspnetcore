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
            if (context.ActionArguments["fromGlobalActionFilter"] == null)
            {
                context.ActionArguments["fromGlobalActionFilter"] = new List<ContentResult>();
            }
            (context.ActionArguments["fromGlobalActionFilter"] as List<ContentResult>)
                .Add(Helpers.GetContentResult(context.Result, "Controller override - OnActionExecuting"));
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.Result = Helpers.GetContentResult(context.Result, "Controller override - OnActionExecuted");
        }
    }
}