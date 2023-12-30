// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Discovery;

internal interface IRazorComponentApplication
{
    ComponentApplicationBuilder GetBuilder();

    static ComponentApplicationBuilder GetBuilderForAssembly(ComponentApplicationBuilder builder, Assembly assembly)
    {
        var libraryName = assembly.FullName!;
        var (pages, components) = CreatePageRouteCollection(libraryName, assembly);
        builder.AddLibrary(new AssemblyComponentLibraryDescriptor(libraryName, pages, components));
        return builder;

        static (IReadOnlyList<PageComponentBuilder>, IReadOnlyList<ComponentBuilder>) CreatePageRouteCollection(string name, Assembly assembly)
        {
            var exported = assembly.GetExportedTypes();
            var pages = new List<PageComponentBuilder>();
            var components = new List<ComponentBuilder>();

            for (var i = 0; i < exported.Length; i++)
            {
                var candidate = exported[i];
                if (candidate.IsAssignableTo(typeof(IComponent)))
                {
                    if (candidate.GetCustomAttributes<RouteAttribute>() is { } routes &&
                        routes.Any())
                    {
                        pages.Add(new PageComponentBuilder()
                        {
                            RouteTemplates = routes.Select(r => r.Template).ToList(),
                            AssemblyName = name,
                            PageType = candidate
                        });
                    }

                    var renderMode = candidate.GetCustomAttribute<RenderModeAttribute>();
                    components.Add(new ComponentBuilder() { AssemblyName = name, ComponentType = candidate, RenderMode = renderMode });
                }
            }

            return (pages, components);
        }
    }
}
