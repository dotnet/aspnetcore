// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Describes a component: how to construct it, which of its properties are parameters or injected
/// services, and the attribute-shaped facts about it that routing and rendering read.
/// </summary>
/// <remarks>
/// <para>
/// Instances are produced by the Blazor Native AOT metadata source generator and reached through the
/// application's metadata context. A component the generator could not describe completely has no
/// descriptor at all, in which case the framework reflects over it exactly as it does today.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The generated form of a routable interactive page:
/// <code>
/// new ComponentDescriptor
/// {
///     Type = typeof(Counter),
///     CreateInstance = static sp =&gt; new Counter(),
///     Metadata = [new RouteAttribute("/counter")],
/// }
/// </code>
/// </example>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ComponentDescriptor
{
    /// <summary>
    /// Gets the component <see cref="Type"/>.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public required Type Type { get; init; }

    /// <summary>
    /// Gets the delegate that constructs the component, or <see langword="null"/> when the generator
    /// could not observe a usable constructor.
    /// </summary>
    /// <remarks>
    /// Takes an <see cref="IServiceProvider"/> because components support constructor injection: the
    /// generated form is the compile-time equivalent of the object factory the framework builds today,
    /// resolving each constructor parameter from the provider. When this is <see langword="null"/> the
    /// framework falls back to its existing activation path.
    /// </remarks>
    public Func<IServiceProvider, IComponent>? CreateInstance { get; init; }

    /// <summary>
    /// Gets the descriptors for the component's parameters and cascading parameters.
    /// </summary>
    public IReadOnlyList<ComponentParameterDescriptor> Parameters { get; init; } = [];

    /// <summary>
    /// Gets the descriptors for the component's injected properties.
    /// </summary>
    public IReadOnlyList<ComponentInjectableDescriptor> Injectables { get; init; } = [];

    /// <summary>
    /// Gets the attribute instances that describe the component, such as its route templates, its
    /// render mode and its layout.
    /// </summary>
    /// <remarks>
    /// A bag of attribute instances rather than named members, because that is the shape the discovery
    /// layer already consumes: a consumer filters it with <see cref="System.Linq.Enumerable.OfType{T}"/>
    /// exactly as it filters the result of a reflective attribute lookup today. This keeps the
    /// descriptor open to a new routing or rendering attribute without an API change.
    /// </remarks>
    public IReadOnlyList<object> Metadata { get; init; } = [];

    private string GetDebuggerDisplay()
        => $"Type = {Type.FullName}, Parameters = {Parameters.Count}, Injectables = {Injectables.Count}";
}
