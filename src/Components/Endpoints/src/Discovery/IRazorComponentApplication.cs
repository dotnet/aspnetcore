// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The definition of a Razor Components Application.
/// </summary>
/// <remarks>
/// Typically the top level component (like the App component or the MainLayout component)
/// for the application implements this interface.
/// </remarks>
/// <typeparam name="TComponent">The component used to define the application.</typeparam>
public interface IRazorComponentApplication<TComponent>
    where TComponent : IRazorComponentApplication<TComponent>
{
    /// <summary>
    /// Creates a builder that can be used to customize the definition of the application.
    /// For example, to add or remove pages, change routes, etc.
    /// </summary>
    /// <returns>
    /// The <see cref="ComponentApplicationBuilder"/> associated with the application
    /// definition.
    /// </returns>
    static virtual ComponentApplicationBuilder GetBuilder()
    {
        var builder = new ComponentApplicationBuilder();
        // TODO: We'll have to figure out the exact API shape here
        // once we support discovery, since this will be generated on the user
        // app and will be public.
        // Similarly, RegisterPages will have to be called by the user app code
        // with the source generated code.
        // The builder provides the ability to choose what assemblies will be
        // considered and checked as a source for endpoints.
        // We also want to have a bit more granularity and exclude/replace individual
        // pages from within a given assembly.
        // We don't know if we need a generic "Feature" abstraction that can be registered
        // or if we can instead extend the builder with new methods whenever we need to add
        // new functionality.
        // The builder is going to be responsible for generating the code that replaces
        // scanning and reflection wherever we do it, so it needs to be something that
        // the user can configure.
        // In general, I want to avoid the "everything is a feature" for things like Pages.
        // The way to tweak any aspect that affect a page is through conventions in the
        // endpoint metadata.
        // We might expose something like the PageCollection or PageFeature in the future
        // so that users can decide the list of things that get considered as endpoints.
        builder.AddAssembly(typeof(TComponent).Assembly.FullName!);
        builder.RegisterPages(new PageCollection(CreatePageRouteCollection()));

        return builder;

        static IEnumerable<PageDefinition> CreatePageRouteCollection()
        {
            var exported = typeof(TComponent).Assembly.GetExportedTypes();
            for (var i = 0; i < exported.Length; i++)
            {
                var candidate = exported[i];
                if (candidate.IsAssignableTo(typeof(IComponent)) &&
                    // Note that this does not support multiple routes, which is
                    // something someone could do with an explicit [Route] attribute
                    // definition.
                    candidate.GetCustomAttribute<RouteAttribute>() is { } route)
                {
                    yield return new PageDefinition(
                        candidate.FullName!,
                        candidate,
                        route.Template,
                        // We remove the route attribute since it is captured on the endpoint.
                        // This is similar to how MVC behaves.
                        candidate.GetCustomAttributes(inherit: true).Except(Enumerable.Repeat(route, 1)).ToArray());
                };
            }
        }
    }
}
