// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Describes the members and indexers of a form model type that a binding expression may traverse,
/// so that the framework can evaluate the expression without compiling a delegate.
/// </summary>
/// <remarks>
/// <para>
/// A <c>@bind</c> or <c>For</c> expression is a chain of member accesses rooted in a constant. The
/// framework anchors that chain at the node whose static type matches the type of the edit context's
/// model, takes that node's value from the edit context, and then evaluates one hop at a time through
/// the descriptors here. Only the model graph needs describing: the component half of the chain is
/// anchored rather than walked.
/// </para>
/// <para>
/// Instances are produced by the Blazor Native AOT metadata source generator for every type named by a
/// <c>BindableModelAttribute</c> and every type reachable from it.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
/// <example>
/// The descriptor for a model exposing <c>Address</c> and <c>Orders</c>:
/// <code>
/// new BindableTypeDescriptor
/// {
///     Type = typeof(LoginModel),
///     Members =
///     [
///         new BindableMemberDescriptor
///         {
///             Name = "Address",
///             MemberType = typeof(Address),
///             GetValue = static target =&gt; ((LoginModel)target).Address,
///         },
///     ],
/// }
/// </code>
/// </example>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class BindableTypeDescriptor
{
    /// <summary>
    /// Gets the type being described.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// Gets the describable instance fields and properties of <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Covers fields as well as properties, because a model may expose either and the expression walk
    /// does not distinguish between them.
    /// </remarks>
    public IReadOnlyList<BindableMemberDescriptor> Members { get; init; } = [];

    /// <summary>
    /// Gets the single-argument indexers declared on <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// Keyed by index type, because a type may declare both <c>this[int]</c> and <c>this[string]</c>.
    /// Array indexing needs no descriptor.
    /// </remarks>
    public IReadOnlyList<BindableIndexerDescriptor> Indexers { get; init; } = [];
}
