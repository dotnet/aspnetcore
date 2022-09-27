// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.Filters;

internal sealed class PageViewDataAttributeFilterFactory : IFilterFactory
{
    public PageViewDataAttributeFilterFactory(IReadOnlyList<LifecycleProperty> properties)
    {
        Properties = properties;
    }

    public IReadOnlyList<LifecycleProperty> Properties { get; }

    // PageViewDataAttributeFilter is stateful and cannot be reused.
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new PageViewDataAttributeFilter(Properties);
    }
}
