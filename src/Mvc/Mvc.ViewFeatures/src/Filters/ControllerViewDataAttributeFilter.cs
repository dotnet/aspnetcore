// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

internal sealed class ControllerViewDataAttributeFilter : IActionFilter, IViewDataValuesProviderFeature
{
    public ControllerViewDataAttributeFilter(IReadOnlyList<LifecycleProperty> properties)
    {
        Properties = properties;
    }

    public object Subject { get; set; }

    public IReadOnlyList<LifecycleProperty> Properties { get; }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        Subject = context.Controller;
        context.HttpContext.Features.Set<IViewDataValuesProviderFeature>(this);
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
