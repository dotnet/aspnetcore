// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class PageSaveTempDataPropertyFilter : SaveTempDataPropertyFilterBase, IPageFilter
    {
        public PageSaveTempDataPropertyFilter(ITempDataDictionaryFactory factory)
            : base(factory)
        {
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            Subject = context.HandlerInstance;
            var tempData = _tempDataFactory.GetTempData(context.HttpContext);

            SetPropertyValues(tempData);
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}
