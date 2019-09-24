// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class ControllerSaveTempDataPropertyFilter : SaveTempDataPropertyFilterBase, IActionFilter
    {
        public ControllerSaveTempDataPropertyFilter(ITempDataDictionaryFactory factory)
            : base(factory)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        /// <inheritdoc />
        public void OnActionExecuting(ActionExecutingContext context)
        {
            Subject = context.Controller;
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);

            SetPropertyValues(tempData);
        }
    }
}
