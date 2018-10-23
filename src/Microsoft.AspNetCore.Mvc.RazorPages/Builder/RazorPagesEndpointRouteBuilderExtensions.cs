// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class RazorPagesEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapRazorPages(
            this IEndpointRouteBuilder routeBuilder,
            string basePath = null)
        {
            var mvcEndpointDataSource = routeBuilder.DataSources.OfType<MvcEndpointDataSource>().FirstOrDefault();

            if (mvcEndpointDataSource == null)
            {
                mvcEndpointDataSource = routeBuilder.ServiceProvider.GetRequiredService<MvcEndpointDataSource>();
                routeBuilder.DataSources.Add(mvcEndpointDataSource);
            }

            var conventionBuilder = new DefaultEndpointConventionBuilder();

            mvcEndpointDataSource.AttributeRoutingConventionResolvers.Add(actionDescriptor =>
            {
                if (actionDescriptor is PageActionDescriptor pageActionDescriptor)
                {
                    // TODO: Filter pages by path
                    return conventionBuilder;
                }

                return null;
            });

            return conventionBuilder;
        }
    }
}
