// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Builder used to configure a razor component application.
/// </summary>
internal class ComponentApplicationBuilder
{
    private readonly HashSet<string> _assemblies = new();

    internal PageCollectionBuilder Pages { get; } = new PageCollectionBuilder();

    internal ComponentCollectionBuilder Components { get; } = new ComponentCollectionBuilder();

    /// <summary>
    /// Builds the component application definition.
    /// </summary>
    /// <returns>The <see cref="RazorComponentApplication"/>.</returns>
    internal RazorComponentApplication Build()
    {
        return new RazorComponentApplication(
            Pages.ToPageCollection(),
            Components.ToComponentCollection());
    }

    /// <summary>
    /// Indicates whether the current <see cref="ComponentApplicationBuilder"/> instance
    /// has the given <paramref name="assemblyName"/>.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to check.</param>
    /// <returns><c>true</c> when present; <c>false</c> otherwise.</returns>
    public bool HasAssembly(string assemblyName)
    {
        return _assemblies.Contains(assemblyName);
    }

    /// <summary>
    /// Scans the given assembly for pages and adds them to the current set of pages.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to scan for pages.</param>
    /// <returns>The <see cref="ComponentApplicationBuilder"/>.</returns>
    public ComponentApplicationBuilder AddAssembly(Assembly assembly)
    {
        var builder = IRazorComponentApplication.GetBuilderForAssembly(this, assembly);
        this.Combine(builder);

        return this;
    }

    /// <summary>
    /// Removes all the discovered pages that are part of the given assembly.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to remove.</param>
    /// <returns>The <see cref="ComponentApplicationBuilder"/>.</returns>
    public ComponentApplicationBuilder RemoveAssembly(Assembly assembly)
    {
        this.RemoveLibrary(assembly.GetName().Name!);
        return this;
    }

    /// <summary>
    /// Adds a given assembly and associated pages and components to the build.
    /// </summary>
    /// <param name="libraryBuilder">The assembly with the pages and components.</param>
    /// <exception cref="InvalidOperationException">When the assembly has already been added
    /// to this component application builder.
    /// </exception>
    internal void AddLibrary(AssemblyComponentLibraryDescriptor libraryBuilder)
    {
        if (_assemblies.Contains(libraryBuilder.AssemblyName))
        {
            throw new InvalidOperationException("Assembly already defined.");
        }
        _assemblies.Add(libraryBuilder.AssemblyName);
        Pages.AddFromLibraryInfo(libraryBuilder.AssemblyName, libraryBuilder.Pages);
        Components.AddFromLibraryInfo(libraryBuilder.AssemblyName, libraryBuilder.Components);
    }

    /// <summary>
    /// Combines the two <see cref="ComponentApplicationBuilder"/> instances.
    /// </summary>
    /// <param name="other">The <see cref="ComponentApplicationBuilder"/> to merge.</param>
    internal void Combine(ComponentApplicationBuilder other)
    {
        _assemblies.UnionWith(other._assemblies);
        Pages.Combine(other.Pages);
        Components.Combine(other.Components);
    }

    /// <summary>
    /// Excludes the assemblies and other definitions in <paramref name="builder"/> from the
    /// current <see cref="ComponentApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder"></param>
    internal void Exclude(ComponentApplicationBuilder builder)
    {
        _assemblies.ExceptWith(builder._assemblies);
        Pages.Exclude(builder.Pages);
        Components.Exclude(builder.Components);
    }

    /// <summary>
    /// Removes the given <paramref name="assembly"/> and the associated definitions from
    /// the current <see cref="ComponentApplicationBuilder"/>.
    /// </summary>
    /// <param name="assembly">The assembly name.</param>
    internal void RemoveLibrary(string assembly)
    {
        _assemblies.Remove(assembly);
        Pages.RemoveFromAssembly(assembly);
        Components.Remove(assembly);
    }

    /// <summary>
    /// Gets the <see cref="ComponentApplicationBuilder"/> for the given <typeparamref name="TComponent"/>.
    /// </summary>
    /// <typeparam name="TComponent">A component inside the assembly.</typeparam>
    /// <returns></returns>
    internal static ComponentApplicationBuilder? GetBuilder<TComponent>()
    {
        var assembly = typeof(TComponent).Assembly;
        var attribute = assembly.GetCustomAttribute<RazorComponentApplicationAttribute>();

        return attribute?.GetBuilder();
    }
}
