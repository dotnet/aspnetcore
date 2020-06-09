// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class PageViewDataAttributeFilter : IPageFilter, IViewDataValuesProviderFeature
    {
        public PageViewDataAttributeFilter(IReadOnlyList<LifecycleProperty> properties)
        {
            Properties = properties;
        }

        public IReadOnlyList<LifecycleProperty> Properties { get;  }

        public object Subject { get; set; }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            Subject = context.HandlerInstance;
            context.HttpContext.Features.Set<IViewDataValuesProviderFeature>(this);
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void ProvideViewDataValues(ViewDataDictionary viewData)
        {
            for (var i = 0; i < Properties.Count; i++)
            {
                var property = Properties[i];
                var value = property.GetValue(Subject);

                if (value != null)
                {
                    viewData[property.Key] = value;
                }
            }
        }
    }
}
