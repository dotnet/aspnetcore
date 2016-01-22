// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class ModifiedRandomNumberProvider : IActionFilter
    {
        private DummyService _dummyService;

        public ModifiedRandomNumberProvider(DummyService dummyService)
        {
            _dummyService = dummyService;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.ActionArguments["randomNumber"] = _dummyService.RandomNumber;
        }
    }
}