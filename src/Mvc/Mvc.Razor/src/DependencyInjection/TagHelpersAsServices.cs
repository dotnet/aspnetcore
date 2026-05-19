// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

internal static class TagHelpersAsServices
{
    public static void AddTagHelpersAsServices(ApplicationPartManager manager, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(services);

        var feature = new TagHelperFeature();
        manager.PopulateFeature(feature);

        foreach (var type in feature.TagHelpers.Select(t => t.AsType()))
        {
            services.TryAddTransient(type, type);
        }

        services.Replace(ServiceDescriptor.Transient<ITagHelperActivator, ServiceBasedTagHelperActivator>());
    }
}
