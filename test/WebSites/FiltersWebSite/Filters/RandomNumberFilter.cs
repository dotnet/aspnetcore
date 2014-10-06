// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace FiltersWebSite
{
    public class RandomNumberFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            context.Result = new ContentResult()
            {
                Content = "4",
                ContentType = "text/plain"
            };
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}