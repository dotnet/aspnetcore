// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Builder used to configure a razor component application.
/// </summary>
internal class ComponentApplicationBuilder
{
    private readonly IComponentTypeInfoResolver _typeInfoResolver;
    private readonly Dictionary<string, IReadOnlyList<ComponentTypeInfo>> _assemblies = new();

    internal ComponentApplicationBuilder()
        : this(ComponentTypeInfoResolverFactory.Default)
    {
    }

    internal ComponentApplicationBuilder(IComponentTypeInfoResolver typeInfoResolver)
    {
        _typeInfoResolver = typeInfoResolver;
    }

    /// <summary>
    /// Builds the component application definition.
    /// </summary>
    /// <returns>The <see cref="RazorComponentApplication"/>.</returns>
    internal RazorComponentApplication Build()
    {
        var pages = new List<PageComponentDescriptor>();
        var components = new List<ComponentDescriptor>();

        foreach (var typeInfos in _assemblies.Values)
        {
            foreach (var typeInfo in typeInfos)
            {
                components.Add(typeInfo.Descriptor);

                List<string>? routes = null;
                foreach (var item in typeInfo.Metadata)
                {
                    if (item is RouteAttribute route)
                    {
                        routes ??= [];
                        routes.Add(route.Template);
                    }
                }

                if (routes is null)
                {
                    continue;
                }

                var endpointMetadata = typeInfo.Metadata
                    .Where(static item => item is not RouteAttribute)
                    .ToArray();

                foreach (var route in routes)
                {
                    pages.Add(new PageComponentDescriptor(route, typeInfo.Type, route, endpointMetadata));
                }
            }
        }

        return new RazorComponentApplication([.. pages], [.. components]);
    }

    /// <summary>
    /// Indicates whether the current <see cref="ComponentApplicationBuilder"/> instance
    /// has the given <paramref name="assemblyName"/>.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to check.</param>
    /// <returns><c>true</c> when present; <c>false</c> otherwise.</returns>
    public bool HasAssembly(string assemblyName)
    {
        return _assemblies.ContainsKey(assemblyName);
    }

    /// <summary>
    /// Discovers pages from the given assembly and adds them to the current set of pages.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to discover pages from.</param>
    /// <returns>The <see cref="ComponentApplicationBuilder"/>.</returns>
    public ComponentApplicationBuilder AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        AddLibrary(assembly.FullName!, _typeInfoResolver.GetRequiredTypeInfos(assembly));

        return this;
    }

    /// <summary>
    /// Removes all the discovered pages that are part of the given assembly.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to remove.</param>
    /// <returns>The <see cref="ComponentApplicationBuilder"/>.</returns>
    public ComponentApplicationBuilder RemoveAssembly(Assembly assembly)
    {
        this.RemoveLibrary(assembly.FullName!);
        return this;
    }

    /// <summary>
    /// Adds a given assembly and its resolved components to the build.
    /// </summary>
    /// <param name="assemblyName">The assembly name.</param>
    /// <param name="typeInfos">The resolved component metadata.</param>
    /// <exception cref="InvalidOperationException">When the assembly has already been added
    /// to this component application builder.
    /// </exception>
    internal void AddLibrary(string assemblyName, IReadOnlyList<ComponentTypeInfo> typeInfos)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);
        ArgumentNullException.ThrowIfNull(typeInfos);

        if (_assemblies.ContainsKey(assemblyName))
        {
            throw new InvalidOperationException("Assembly already defined.");
        }

        _assemblies.Add(assemblyName, typeInfos);
    }

    /// <summary>
    /// Combines the two <see cref="ComponentApplicationBuilder"/> instances.
    /// </summary>
    /// <param name="other">The <see cref="ComponentApplicationBuilder"/> to merge.</param>
    internal void Combine(ComponentApplicationBuilder other)
    {
        foreach (var (assemblyName, typeInfos) in other._assemblies)
        {
            _assemblies.TryAdd(assemblyName, typeInfos);
        }
    }

    /// <summary>
    /// Excludes the assemblies and other definitions in <paramref name="builder"/> from the
    /// current <see cref="ComponentApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder"></param>
    internal void Exclude(ComponentApplicationBuilder builder)
    {
        foreach (var assemblyName in builder._assemblies.Keys)
        {
            _assemblies.Remove(assemblyName);
        }
    }

    /// <summary>
    /// Removes the given <paramref name="assembly"/> and the associated definitions from
    /// the current <see cref="ComponentApplicationBuilder"/>.
    /// </summary>
    /// <param name="assembly">The assembly name.</param>
    internal void RemoveLibrary(string assembly)
    {
        _assemblies.Remove(assembly);
    }
}
