// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// A default implementation of <see cref="IUrlHelperFactory"/>.
/// </summary>
public class UrlHelperFactory : IUrlHelperFactory
{
    /// <inheritdoc />
    public IUrlHelper GetUrlHelper(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
        if (httpContext.Items.TryGetValue(typeof(IUrlHelper), out var value) && value is IUrlHelper urlHelper)
        {
            return urlHelper;
        }

        var endpoint = httpContext.GetEndpoint();
        if (endpoint != null)
        {
            var services = httpContext.RequestServices;
            var linkGenerator = services.GetRequiredService<LinkGenerator>();
            var endpointDataSource = services.GetRequiredService<EndpointDataSource>();

            urlHelper = new EndpointRoutingUrlHelper(
                context,
                linkGenerator,
                endpointDataSource);
        }
        else
        {
            urlHelper = new UrlHelper(context);
        }

        httpContext.Items[typeof(IUrlHelper)] = urlHelper;

        return urlHelper;
    }
}
