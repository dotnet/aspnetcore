// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageSaveTempDataPropertyFilter : SaveTempDataPropertyFilterBase, IPageFilter
    {
        public PageSaveTempDataPropertyFilter(ITempDataDictionaryFactory factory)
            : base(factory)
        {
        }

        public PageSaveTempDataPropertyFilterFactory FilterFactory { get; set; }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            if (context.HandlerInstance == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(PageHandlerExecutingContext.HandlerInstance),
                    typeof(PageHandlerExecutingContext).Name));
            }

            if (FilterFactory == null)
            {
                throw new InvalidOperationException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(FilterFactory),
                    typeof(PageSaveTempDataPropertyFilter).Name));
            }

            var tempData = _factory.GetTempData(context.HttpContext);

            Subject = context.HandlerInstance;
            Properties = FilterFactory.GetTempDataProperties(Subject.GetType());

            SetPropertyVaules(tempData, Subject);
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}
