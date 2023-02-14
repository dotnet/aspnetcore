// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Contains extension methods for using Razor Components with <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class RazorComponentsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Adds endpoints for Razor Components to the <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <returns>An <see cref="PageActionEndpointConventionBuilder"/> for endpoints associated with Razor Components.</returns>
    public static PageActionEndpointConventionBuilder MapRazorComponents(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        EnsureRazorComponentsServices(endpoints);

        var dataSource = PageActionEndpointDataSourceProvider.GetOrCreateDataSource(endpoints);

        MapComponentsRoutesToStaticallyRenderedHtml(endpoints);

        return dataSource.DefaultBuilder;
    }

    private static void EnsureRazorComponentsServices(IEndpointRouteBuilder endpoints)
    {
        var marker = endpoints.ServiceProvider.GetService<PageActionEndpointDataSourceFactory>();
        if (marker == null)
        {
            throw new InvalidOperationException(Mvc.Core.Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                "AddRazorComponents",
                "ConfigureServices(...)"));
        }
    }

    private static void MapComponentsRoutesToStaticallyRenderedHtml(IEndpointRouteBuilder endpoints)
    {
        var entryAssembly = Assembly.GetEntryAssembly()!;
        var componentTypes = entryAssembly.ExportedTypes.Where(t => typeof(IComponent).IsAssignableFrom(t));
        foreach (var componentType in componentTypes)
        {
            if (componentType.GetCustomAttribute<RouteAttribute>() is RouteAttribute routeAttribute)
            {
                endpoints.Map(routeAttribute.Template, httpContext =>
                {
                    var renderer = httpContext.RequestServices.GetRequiredService<PassiveComponentRenderer>();
                    return renderer.HandleRequest(httpContext, componentType);
                });
            }
        }
    }
}
