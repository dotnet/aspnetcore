// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public static class TagHelpersAsServices
    {
        public static void AddTagHelpersAsServices(ApplicationPartManager manager, IServiceCollection services)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var feature = new TagHelperFeature();
            manager.PopulateFeature(feature);

            foreach (var type in feature.TagHelpers.Select(t => t.AsType()))
            {
                services.TryAddTransient(type, type);
            }

            services.Replace(ServiceDescriptor.Transient<ITagHelperActivator, ServiceBasedTagHelperActivator>());
        }
    }
}
