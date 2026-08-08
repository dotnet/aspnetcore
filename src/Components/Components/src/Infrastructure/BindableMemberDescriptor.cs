// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Describes one instance field or property of a form model type, so that a binding expression can
/// traverse it without compiling a delegate.
/// </summary>
/// <remarks>
/// <para>
/// Reached through <see cref="BindableTypeDescriptor.Members"/>.
/// </para>
/// <para>
/// This API is experimental, unsupported, and subject to change or removal in any release.
/// </para>
/// </remarks>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public sealed class BindableMemberDescriptor
{
    /// <summary>
    /// Gets the name of the field or property, used to match the member the expression names.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the declared type of the member, which is the type of the next hop in the chain.
    /// </summary>
    public required Type MemberType { get; init; }

    /// <summary>
    /// Gets the delegate that reads the member's value from an instance of the declaring type.
    /// </summary>
    public required Func<object, object?> GetValue { get; init; }
}
