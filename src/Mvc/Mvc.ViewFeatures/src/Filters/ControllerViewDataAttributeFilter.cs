// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class ControllerViewDataAttributeFilter : IActionFilter, IViewDataValuesProviderFeature
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
}
