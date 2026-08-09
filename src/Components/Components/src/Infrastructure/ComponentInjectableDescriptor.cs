// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Describes a single <see cref="InjectAttribute"/> property of a component, together with the delegate
/// that assigns the resolved service to it.
/// </summary>
/// <remarks>
/// <para>
/// Instances are produced by the Blazor Native AOT metadata source generator and reached through
/// <see cref="ComponentDescriptor.Injectables"/>. Injection is a distinct role from parameter binding:
/// there is no getter, no cascading attribute, and no unmatched-value behavior, which is why it has its
/// own descriptor rather than sharing one with a discriminator.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The generated form of an <c>[Inject] public NavigationManager Nav { get; set; }</c> property:
/// <code>
/// new ComponentInjectableDescriptor
/// {
///     Name = "Nav",
///     ServiceType = typeof(NavigationManager),
///     Attribute = new InjectAttribute(),
///     SetValue = static (target, value) =&gt; ((Counter)target).Nav = (NavigationManager)value!,
/// }
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class ComponentInjectableDescriptor
{
    internal bool HasSetter { get; init; } = true;

    /// <summary>
    /// Gets the name of the property, used only to build the error message raised when the service
    /// cannot be resolved.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the declared type of the property, which is the service type to resolve.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets the <see cref="InjectAttribute"/> applied to the property.
    /// </summary>
    /// <remarks>
    /// Carries <see cref="InjectAttribute.Key"/>, so a keyed service resolves through the keyed
    /// service provider APIs.
    /// </remarks>
    public required InjectAttribute Attribute { get; init; }

    /// <summary>
    /// Gets the delegate that assigns the resolved service to the property on a component instance.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a member reference so that the generator can choose how to reach the
    /// property: a direct assignment for a public property, or an
    /// <see cref="System.Runtime.CompilerServices.UnsafeAccessorAttribute"/> setter for a protected one.
    /// Neither form introduces reflection or runtime code generation.
    /// </remarks>
    public required Action<object, object?> SetValue { get; init; }
}
