// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class RandomNumberProvider : IActionFilter
    {
        private RandomNumberService _random;

        public RandomNumberProvider(RandomNumberService random)
        {
            _random = random;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.ActionArguments["randomNumber"] = _random.GetRandamNumber();
        }
    }
}