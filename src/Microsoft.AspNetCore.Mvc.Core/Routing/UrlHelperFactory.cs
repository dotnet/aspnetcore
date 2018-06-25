// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.EndpointFinders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    /// <summary>
    /// A default implementation of <see cref="IUrlHelperFactory"/>.
    /// </summary>
    public class UrlHelperFactory : IUrlHelperFactory
    {
        /// <inheritdoc />
        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(Resources.ArgumentCannotBeNullOrEmpty, (nameof(context)));
            }

            var httpContext = context.HttpContext;

            if (httpContext == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(ActionContext.HttpContext),
                    nameof(ActionContext)));
            }

            if (httpContext.Items == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(HttpContext.Items),
                    nameof(HttpContext)));
            }

            // Perf: Create only one UrlHelper per context
            if (httpContext.Items.TryGetValue(typeof(IUrlHelper), out var value) && value is IUrlHelper)
            {
                return (IUrlHelper)value;
            }

            IUrlHelper urlHelper;
            var endpointFeature = httpContext.Features.Get<IEndpointFeature>();
            if (endpointFeature?.Endpoint != null)
            {
                var services = httpContext.RequestServices;
                var linkGenerator = services.GetRequiredService<ILinkGenerator>();
                var routeValuesBasedEndpointFinder = services.GetRequiredService<IEndpointFinder<RouteValuesBasedEndpointFinderContext>>();
                var logger = services.GetRequiredService<ILogger<DispatcherUrlHelper>>();

                urlHelper = new DispatcherUrlHelper(
                    context,
                    routeValuesBasedEndpointFinder,
                    linkGenerator,
                    logger);
            }
            else
            {
                urlHelper = new UrlHelper(context);
            }

            httpContext.Items[typeof(IUrlHelper)] = urlHelper;

            return urlHelper;
        }
    }
}