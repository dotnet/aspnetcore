// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// TODO
/// </summary>
public static class RazorComponentsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// TODO: Move this to an appropriate place
    /// </summary>
    public static void AddRazorComponents(this IServiceCollection services)
    {
        // TODO: It might be possible to add fewer services here
        services
            .AddMvcCore()
            .AddAuthorization()
            .AddDataAnnotations()
            .AddRazorPages();

        // TODO: Create a better way to get the passive request's HTTP context
        services.AddHttpContextAccessor();

        services.AddScoped<PassiveComponentRenderer>();
    }

    /// <summary>
    /// TODO
    /// </summary>
    public static void MapRazorComponents(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // TODO: Implement this like MapRazorPages, which is vastly more complex (assuming there are good reasons)
        var entryAssembly = Assembly.GetEntryAssembly()!;
        var componentTypes = entryAssembly.ExportedTypes.Where(t => typeof(IComponent).IsAssignableFrom(t));
        foreach (var componentType in componentTypes)
        {
            if (componentType.GetCustomAttribute<RouteAttribute>() is RouteAttribute routeAttribute)
            {
                endpoints.Map(routeAttribute.Template, httpContext =>
                {
                    var renderer = httpContext.RequestServices.GetRequiredService<PassiveComponentRenderer>();
                    return renderer.HandleRequest(httpContext, ComponentRenderMode.Unspecified, componentType, parameters: null);
                });
            }
        }
    }
}
