// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

internal sealed class ControllerSaveTempDataPropertyFilterFactory : IFilterFactory
{
    public ControllerSaveTempDataPropertyFilterFactory(IReadOnlyList<LifecycleProperty> properties)
    {
        TempDataProperties = properties;
    }

    public IReadOnlyList<LifecycleProperty> TempDataProperties { get; }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var service = serviceProvider.GetRequiredService<ControllerSaveTempDataPropertyFilter>();
        service.Properties = TempDataProperties;
        return service;
    }
}
