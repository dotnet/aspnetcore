// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class PageViewDataAttributeFilter : IPageFilter, IViewDataValuesProviderFeature
{
    public PageViewDataAttributeFilter(IReadOnlyList<LifecycleProperty> properties)
    {
        Properties = properties;
    }

    public IReadOnlyList<LifecycleProperty> Properties { get; }

    public object? Subject { get; set; }

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
